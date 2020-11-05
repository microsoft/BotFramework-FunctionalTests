// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Rest.TransientFaultHandling;
using Newtonsoft.Json;
using Activity = Microsoft.Bot.Connector.DirectLine.Activity;
using BotActivity = Microsoft.Bot.Schema.Activity;
using BotChannelAccount = Microsoft.Bot.Schema.ChannelAccount;
using ChannelAccount = Microsoft.Bot.Connector.DirectLine.ChannelAccount;

namespace TranscriptTestRunner.TestClients
{
    public class DirectLineTestClient : TestClientBase, IDisposable
    {
        // DL client sample: https://github.com/microsoft/BotFramework-DirectLine-DotNet/tree/main/samples/core-DirectLine/DirectLineClient
        private const string DirectLineSecretKey = "DIRECTLINE";
        private const string BotIdKey = "BOTID";
        private const string OriginHeaderKey = "Origin";
        private const string OriginHeaderValue = "https://botframework.test.com";
        private static string _botId;
        private readonly ConcurrentQueue<BotActivity> _activityQueue = new ConcurrentQueue<BotActivity>();
        private readonly DirectLineClient _dlClient;
        private readonly string _user = $"TestUser-{Guid.NewGuid()}";
        private readonly string _token;
        private readonly string _conversationId;
        private bool _conversationStarted;

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

            // Instead of generating a vanilla DirectLineClient with secret, 
            // we obtain a directLine token with the secrets and then we use
            // that token to create the directLine client.
            // What this gives us is the ability to pass TrustedOrigins when obtaining the token,
            // which tests the enhanced authentication.
            // This endpoint is unfortunately not supported by the directLine client which is 
            // why we add this custom code.
            using var client = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://directline.botframework.com/v3/directline/tokens/generate");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", directLineSecret);
            request.Content = new StringContent(
                JsonConvert.SerializeObject(new
                {
                    User = new { Id = _user },
                    TrustedOrigins = new[] { OriginHeaderValue }
                }), Encoding.UTF8,
                "application/json");

            using var response = client.SendAsync(request).GetAwaiter().GetResult();
            if (response.IsSuccessStatusCode)
            {
                // Extract token from response
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                _token = JsonConvert.DeserializeObject<DirectLineToken>(body).Token;
                _conversationId = JsonConvert.DeserializeObject<DirectLineToken>(body).ConversationId;

                // Create directLine client from token
                _dlClient = new DirectLineClient(_token);
                _dlClient.SetRetryPolicy(new RetryPolicy(new HttpStatusCodeErrorDetectionStrategy(), 0));

                // From now on, we'll add an Origin header in directLine calls, with 
                // the trusted origin we sent when acquiring the token as value.
                _dlClient.HttpClient.DefaultRequestHeaders.Add(OriginHeaderKey, OriginHeaderValue);
            }
            else
            {
                throw new Exception("Failed to acquire directLine token");
            }
        }

        public override async Task SendActivityAsync(BotActivity activity, CancellationToken cancellationToken)
        {
            if (!_conversationStarted)
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

        public override async Task SignInAsync(string url)
        {
            var directLineSession = await GetSessionInfoAsync().ConfigureAwait(false);
            var cookieContainer = new CookieContainer();
            using var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                CookieContainer = cookieContainer
            };

            // We have a sign in url, which will produce multiple HTTP 302 for redirects
            // This will path 
            //      token service -> other services -> auth provider -> token service (post sign in)-> response with token
            // When we receive the post sign in redirect, we add the cookie passed in the directLine session info
            // to test enhanced authentication. This in the scenarios happens by itself since browsers do this for us.
            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add(OriginHeaderKey, OriginHeaderValue);

            while (!string.IsNullOrEmpty(url))
            {
                using var response = await client.GetAsync(new Uri(url)).ConfigureAwait(false);
                var text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                url = response.StatusCode == HttpStatusCode.Redirect
                    ? response.Headers.Location.OriginalString
                    : null;

                // Once the redirects are done, there is no more url. This means we 
                // did the entire loop
                if (url == null)
                {
                    if (!response.IsSuccessStatusCode || !text.Contains("You are now signed in and can close this window."))
                    {
                        throw new Exception("An error occurred signing in");
                    }

                    return;
                }

                // If this is the post sign in callback, add the cookie and code challenge
                // so that the token service gets the verification.
                // Here we are simulating what WebChat does along with the browser cookies.
                if (url.StartsWith("https://token.botframework.com/api/oauth/PostSignInCallback", StringComparison.Ordinal))
                {
                    url += $"&code_challenge={directLineSession.SessionId}";
                    cookieContainer.Add(directLineSession.Cookie);
                }
            }

            throw new Exception("Sign in did not succeed. Set a breakpoint in TestBotClient.SignInAsync() to debug the redirect sequence.");
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
            await _dlClient.Conversations.StartConversationAsync().ConfigureAwait(false);
            _conversationStarted = true;
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
                        var botActivity = JsonConvert.DeserializeObject<BotActivity>(JsonConvert.SerializeObject(dlActivity));
                        botActivity.From.Role = RoleTypes.Bot;
                        botActivity.Recipient = new BotChannelAccount(role: RoleTypes.User);
                        _activityQueue.Enqueue(botActivity);
                    }
                }
            }
        }

        private async Task<DirectLineSessionInfo> GetSessionInfoAsync()
        {
            // Set up cookie container to obtain response cookie
            var cookies = new CookieContainer();
            using var handler = new HttpClientHandler { CookieContainer = cookies };

            using var client = new HttpClient(handler);
            
            // Call the directLine session api, not supported by DirectLine client
            const string getSessionUrl = "https://directline.botframework.com/v3/directline/session/getsessionid";
            using var request = new HttpRequestMessage(HttpMethod.Get, getSessionUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

            // We want to add the Origins header to this client as well
            client.DefaultRequestHeaders.Add(OriginHeaderKey, OriginHeaderValue);

            using var response = await client.SendAsync(request).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                // The directLine response that is relevant to us is the cookie and the session info.

                // Extract cookie from cookies
                var cookie = cookies.GetCookies(new Uri(getSessionUrl)).Cast<Cookie>().FirstOrDefault(c => c.Name == "webchat_session_v2");

                // Extract session info from body
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var session = JsonConvert.DeserializeObject<DirectLineSession>(body);

                return new DirectLineSessionInfo
                {
                    SessionId = session.SessionId,
                    Cookie = cookie
                };
            }

            throw new Exception("Failed to obtain session id");
        }
    }
}
