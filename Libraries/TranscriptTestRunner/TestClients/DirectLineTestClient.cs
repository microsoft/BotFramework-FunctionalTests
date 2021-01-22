// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        // Stores the activities received from the bot
        private readonly SortedDictionary<int, BotActivity> _activityQueue = new SortedDictionary<int, BotActivity>();

        // Stores the activities received from the bot that don't immediately correlate with the last activity we received (an activity was skipped)
        private readonly SortedDictionary<int, BotActivity> _futureQueue = new SortedDictionary<int, BotActivity>();

        // Used to lock access to the internal lists
        private readonly object _listLock = new object();

        // Tracks the index of the last activity received
        private int _lastActivityIndex = -1;

        private readonly string _botId;
        private readonly string _directLineSecret;
        private readonly KeyValuePair<string, string> _originHeader = new KeyValuePair<string, string>("Origin", $"https://botframework.test.com/{Guid.NewGuid()}");
        private readonly string _user = $"TestUser-{Guid.NewGuid()}";
        private Conversation _conversation;
        private CancellationTokenSource _webSocketClientCts;

        // To detect redundant calls to dispose
        private bool _disposed;
        private DirectLineClient _dlClient;
        private ClientWebSocket _webSocketClient;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectLineTestClient"/> class.
        /// </summary>
        /// <param name="options">Options for the client configuration.</param>
        /// <param name="logger">The logger.</param>
        public DirectLineTestClient(DirectLineTestClientOptions options, ILogger logger = null)
        {
            if (string.IsNullOrWhiteSpace(options.BotId))
            {
                throw new ArgumentException("BotId not set.");
            }

            _botId = options.BotId;

            if (string.IsNullOrWhiteSpace(options.DirectLineSecret))
            {
                throw new ArgumentException("DirectLineSecret not set.");
            }

            _directLineSecret = options.DirectLineSecret;

            _logger = logger ?? NullLogger.Instance;
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

            // lock the list while work with it.
            lock (_listLock)
            {
                if (_activityQueue.Any())
                {
                    // Return the first activity in the queue (if any)
                    var keyValuePair = _activityQueue.First();
                    _activityQueue.Remove(keyValuePair.Key);

                    return keyValuePair.Value;
                }
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

                _webSocketClientCts?.Cancel();
                _webSocketClientCts?.Dispose();
                _webSocketClient?.Dispose();
            }

            _disposed = true;
        }

        private async Task CreateConversationAsync()
        {
            // Obtain a token using the Direct Line secret
            var tokenInfo = await GetDirectLineTokenAsync().ConfigureAwait(false);

            // Create directLine client from token and initialize settings.
            _dlClient = new DirectLineClient(tokenInfo.Token);
            _dlClient.SetRetryPolicy(new RetryPolicy(new HttpStatusCodeErrorDetectionStrategy(), 0));

            // From now on, we'll add an Origin header in directLine calls, with 
            // the trusted origin we sent when acquiring the token as value.
            _dlClient.HttpClient.DefaultRequestHeaders.Add(_originHeader.Key, _originHeader.Value);

            _webSocketClientCts = new CancellationTokenSource();

            // Start the conversation now the the _dlClient has been initialized.
            _conversation = await _dlClient.Conversations.StartConversationAsync(_webSocketClientCts.Token).ConfigureAwait(false);

            // Initialize web socket client and listener
            _webSocketClient = new ClientWebSocket();
            await _webSocketClient.ConnectAsync(new Uri(_conversation.StreamUrl), _webSocketClientCts.Token).ConfigureAwait(false);
            _ = Task.Factory.StartNew(ListenAsync, _webSocketClientCts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        /// <summary>
        /// This method is invoked as a background task and lists to directline websocket.
        /// </summary>
        private async Task ListenAsync()
        {
            try
            {
                var rcvBytes = new byte[16384];
                var rcvBuffer = new ArraySegment<byte>(rcvBytes);
                while (!_webSocketClientCts.IsCancellationRequested)
                {
                    // Read messages from the socket.
                    string rcvMsg = null;
                    WebSocketReceiveResult rcvResult;
                    do
                    {
                        _logger.LogDebug("Listening to web socket....");
                        rcvResult = await _webSocketClient.ReceiveAsync(rcvBuffer, _webSocketClientCts.Token).ConfigureAwait(false);
                        var msgBytes = rcvBuffer.Skip(rcvBuffer.Offset).Take(rcvResult.Count).ToArray();
                        rcvMsg += Encoding.UTF8.GetString(msgBytes);
                    } 
                    while (!rcvResult.EndOfMessage);

                    _logger.LogDebug("Activity received");
                    _logger.LogDebug(rcvMsg);

                    var activitySet = JsonConvert.DeserializeObject<ActivitySet>(rcvMsg);
                    if (activitySet != null)
                    {
                        // lock the list while work with it.
                        lock (_listLock)
                        {
                            foreach (var dlActivity in activitySet.Activities)
                            {
                                // Convert the DL Activity object to a BF activity object.
                                var botActivity = JsonConvert.DeserializeObject<BotActivity>(JsonConvert.SerializeObject(dlActivity));
                                var activityIndex = int.Parse(botActivity.Id.Split('|')[1], CultureInfo.InvariantCulture);
                                if (activityIndex == _lastActivityIndex + 1)
                                {
                                    ProcessActivity(botActivity, activityIndex);
                                    _lastActivityIndex = activityIndex;
                                }
                                else
                                {
                                    // Activities come out of sequence in some situations. 
                                    // put the activity in the future queue so we can process it once we fill in the gaps.
                                    _futureQueue.Add(activityIndex, botActivity);
                                }
                            }

                            // Process the future queue and append the activities if we filled in the gaps.
                            var queueCopy = new KeyValuePair<int, BotActivity>[_futureQueue.Count];
                            _futureQueue.CopyTo(queueCopy, 0);
                            foreach (var kvp in queueCopy)
                            {
                                if (kvp.Key == _lastActivityIndex + 1)
                                {
                                    ProcessActivity(kvp.Value, kvp.Key);
                                    _futureQueue.Remove(kvp.Key);
                                    _lastActivityIndex = kvp.Key;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("got null set or watermark");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ListenAsync");
                throw;
            }
        }

        private void ProcessActivity(BotActivity botActivity, int activitySeq)
        {
            if (botActivity.From.Id.StartsWith(_botId, StringComparison.CurrentCultureIgnoreCase))
            {
                botActivity.From.Role = RoleTypes.Bot;
                botActivity.Recipient = new BotChannelAccount(role: RoleTypes.User);

                _activityQueue.Add(activitySeq, botActivity);
                _logger.LogDebug($"Added activity to queue. Length: {_activityQueue.Count} - Future activities queue length: {_futureQueue.Count}");
            }
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
    }
}
