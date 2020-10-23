// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Rest.TransientFaultHandling;
using Newtonsoft.Json;
using BotActivity = Microsoft.Bot.Schema.Activity;

namespace TranscriptTestRunner.TestClients
{
    public class DirectLineTestClient : TestClientBase, IDisposable
    {
        // DL client sample: https://github.com/microsoft/BotFramework-DirectLine-DotNet/tree/main/samples/core-DirectLine/DirectLineClient
        private const string DirectLineSecretKey = "DIRECTLINE";
        private const string BotIdKey = "BOTID";
        private static string _botId;
        private readonly ConcurrentQueue<BotActivity> _activityQueue = new ConcurrentQueue<BotActivity>();
        private readonly DirectLineClient _dlClient;
        private readonly string _user = $"TestUser-{Guid.NewGuid()}";
        private string _conversationId;

        // To detect redundant calls to dispose
        private bool _disposed;
        private string _watermark;

        public DirectLineTestClient(IConfiguration config)
        {
            _botId = config[BotIdKey];
            if (string.IsNullOrWhiteSpace(_botId))
            {
                throw new ArgumentException($"Configuration setting '{BotIdKey}' not set.");
            }

            var directLineSecret = config[DirectLineSecretKey];
            if (string.IsNullOrWhiteSpace(directLineSecret))
            {
                throw new ArgumentException($"Configuration setting '{DirectLineSecretKey}' not found.");
            }

            _dlClient = new DirectLineClient(directLineSecret);
            _dlClient.SetRetryPolicy(new RetryPolicy(new HttpStatusCodeErrorDetectionStrategy(), 0));
        }

        public override async Task SendActivityAsync(BotActivity activity, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_conversationId))
            {
                await CreateConversationAsync().ConfigureAwait(false);
            }

            var activityPost = new Activity
            {
                From = new ChannelAccount(_user),
                Text = activity.Text,
                Type = activity.Type
            };

            await _dlClient.Conversations.PostActivityAsync(_conversationId, activityPost, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<BotActivity> GetNextReplyAsync(CancellationToken cancellationToken)
        {
            await PullActivitiesFromDirectLineAsync(cancellationToken).ConfigureAwait(false);

            // Return the first activity in the queue (if any)
            if (_activityQueue.TryDequeue(out var activity))
            {
                return activity;
            }

            // No activities in the queue
            return null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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
            var conversation = await _dlClient.Conversations.StartConversationAsync().ConfigureAwait(false);
            _conversationId = conversation.ConversationId;
        }

        private async Task PullActivitiesFromDirectLineAsync(CancellationToken cancellationToken)
        {
            // Pull all the available activities from direct line
            var activitySet = await _dlClient.Conversations.GetActivitiesAsync(_conversationId, _watermark, cancellationToken).ConfigureAwait(false);

            if (activitySet != null)
            {
                _watermark = activitySet.Watermark;

                // Add activities to the queue
                foreach (var dlActivity in activitySet.Activities)
                {
                    if (dlActivity.From.Id == _botId)
                    {
                        // Convert the DL Activity object to a BF activity object.
                        _activityQueue.Enqueue(JsonConvert.DeserializeObject<BotActivity>(JsonConvert.SerializeObject(dlActivity)));
                    }
                }
            }
        }
    }
}
