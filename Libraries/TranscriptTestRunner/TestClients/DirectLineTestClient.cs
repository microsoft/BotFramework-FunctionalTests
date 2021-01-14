// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.Bot.Schema;
using Microsoft.Rest.TransientFaultHandling;
using Newtonsoft.Json;
using TranscriptTestRunner.Authentication;
using Activity = Microsoft.Bot.Connector.DirectLine.Activity;
using ActivityTypes = Microsoft.Bot.Schema.ActivityTypes;
using BotActivity = Microsoft.Bot.Schema.Activity;
using BotChannelAccount = Microsoft.Bot.Schema.ChannelAccount;
using ChannelAccount = Microsoft.Bot.Connector.DirectLine.ChannelAccount;

namespace TranscriptTestRunner.TestClients
{
    /// <summary>
    /// DirectLine implementation of <see cref="TestClientBase"/>.
    /// </summary>
    public class DirectLineTestClient : TestClientBase, IDisposable
    {
        // DL client sample: https://github.com/microsoft/BotFramework-DirectLine-DotNet/tree/main/samples/core-DirectLine/DirectLineClient
        private readonly ConcurrentQueue<BotActivity> _activityQueue = new ConcurrentQueue<BotActivity>();
        private readonly string _botId;
        private readonly string _directLineSecret;
        private readonly KeyValuePair<string, string> _originHeader = new KeyValuePair<string, string>("Origin", "https://botframework.test.com");
        private readonly string _user = $"TestUser-{Guid.NewGuid()}";
        private Conversation _conversation;

        // To detect redundant calls to dispose
        private bool _disposed;
        private DirectLineClient _dlClient;
        private string _watermark;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectLineTestClient"/> class.
        /// </summary>
        /// <param name="options">Options for the client configuration.</param>
        public DirectLineTestClient(DirectLinetTestClientOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.BotId))
            {
                throw new ArgumentException($"BotId not set.");
            }
            
            _botId = options.BotId;

            if (string.IsNullOrWhiteSpace(options.DirectLineSecret))
            {
                throw new ArgumentException($"DirectLineSecret not set.");
            }
            
            _directLineSecret = options.DirectLineSecret;
        }

        /// <inheritdoc/>
        public override async Task SendActivityAsync(BotActivity activity, CancellationToken cancellationToken)
        {
            if (_conversation == null)
            {
                await CreateConversationAsync().ConfigureAwait(false);

                if (activity.Type == ActivityTypes.ConversationUpdate)
                {
                    // CreateConversationAsync sends a ConversationUpdate automatically.
                    // Ignore the activity sent if it is the first one we are sending to the bot and it is a ConversationUpdate.
                    // This can happen with recorded scripts where we get a conversation update from the transcript that we don't
                    // want to use.
                    return;
                }
            }

            var activityPost = new Activity
            {
                From = new ChannelAccount(_user),
                Text = activity.Text,
                Type = activity.Type
            };

            await _dlClient.Conversations.PostActivityAsync(_conversation.ConversationId, activityPost, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public override async Task<BotActivity> GetNextReplyAsync(CancellationToken cancellationToken)
        {
            if (_conversation == null)
            {
                await CreateConversationAsync().ConfigureAwait(false);
            }

            await PullActivitiesFromDirectLineAsync(cancellationToken).ConfigureAwait(false);

            // Return the first activity in the queue (if any)
            if (_activityQueue.TryDequeue(out var activity))
            {
                return activity;
            }

            // No activities in the queue
            return null;
        }

        /// <inheritdoc/>
        public override async Task<bool> SignInAsync(string url)
        {
            const string sessionUrl = "https://directline.botframework.com/v3/directline/session/getsessionid";
            var directLineSession = await TestClientAuthentication.GetSessionInfoAsync(sessionUrl, _conversation.Token, _originHeader).ConfigureAwait(false);
            return await TestClientAuthentication.SignInAsync(url, _originHeader, directLineSession).ConfigureAwait(false);
        }

        /// <summary>
        /// Frees resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">Boolean value that determines whether to free resources or not.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed objects owned by the class here.
                _dlClient?.Dispose();
            }

            _disposed = true;
        }

        private async Task CreateConversationAsync()
        {
            var tokenInfo = await GetDirectLineTokenAsync().ConfigureAwait(false);

            // Create directLine client from token
            _dlClient = new DirectLineClient(tokenInfo.Token);
            _dlClient.SetRetryPolicy(new RetryPolicy(new HttpStatusCodeErrorDetectionStrategy(), 0));

            // From now on, we'll add an Origin header in directLine calls, with 
            // the trusted origin we sent when acquiring the token as value.
            _dlClient.HttpClient.DefaultRequestHeaders.Add(_originHeader.Key, _originHeader.Value);

            // Start the conversation now the the _dlClient has been initialized.
            _conversation = await _dlClient.Conversations.StartConversationAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Exchanges the directLine secret by an auth token.
        /// </summary>
        /// <remarks>
        /// Instead of generating a vanilla DirectLineClient with secret, 
        /// we obtain a directLine token with the secrets and then we use
        /// that token to create the directLine client.
        /// What this gives us is the ability to pass TrustedOrigins when obtaining the token,
        /// which tests the enhanced authentication.
        /// This endpoint is unfortunately not supported by the directLine client which is 
        /// why we add this custom code.
        /// </remarks>
        /// <returns>A <see cref="TokenInfo"/> instance.</returns>
        private async Task<TokenInfo> GetDirectLineTokenAsync()
        {
            using var client = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://directline.botframework.com/v3/directline/tokens/generate");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _directLineSecret);
            request.Content = new StringContent(
                JsonConvert.SerializeObject(new
                {
                    User = new { Id = _user },
                    TrustedOrigins = new[] { _originHeader.Value }
                }), Encoding.UTF8,
                "application/json");

            using var response = await client.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            // Extract token from response
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var tokenInfo = JsonConvert.DeserializeObject<TokenInfo>(body);
            if (string.IsNullOrWhiteSpace(tokenInfo?.Token))
            {
                throw new InvalidOperationException("Failed to acquire directLine token");
            }

            return tokenInfo;
        }

        private async Task PullActivitiesFromDirectLineAsync(CancellationToken cancellationToken)
        {
            // Pull all the available activities from direct line
            var activitySet = await _dlClient.Conversations.GetActivitiesAsync(_conversation.ConversationId, _watermark, cancellationToken).ConfigureAwait(false);

            if (activitySet != null)
            {
                _watermark = activitySet.Watermark;

                // Add activities to the queue
                foreach (var dlActivity in activitySet.Activities)
                {
                    if (dlActivity.From.Id == _botId)
                    {
                        // Convert the DL Activity object to a BF activity object.
                        var botActivity = JsonConvert.DeserializeObject<BotActivity>(JsonConvert.SerializeObject(dlActivity));
                        botActivity.From.Role = RoleTypes.Bot;
                        botActivity.Recipient = new BotChannelAccount(role: RoleTypes.User);
                        _activityQueue.Enqueue(botActivity);
                    }
                }
            }
        }
    }
}
