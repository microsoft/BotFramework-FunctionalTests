// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Connector.DirectLine;
using Newtonsoft.Json;
using SkillFunctionalTests.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SkillFunctionalTests.Bot
{
    public class TestBotClient
    {
        private const string OriginHeaderKey = "Origin";
        private const string OriginHeaderValue = "https://carlos.test.com";

        private readonly DirectLineClient _directLineClient;
        private readonly IBotTestConfiguration _config;
        private readonly string _user = $"dl_SkillTestUser-{ Guid.NewGuid() }";

        private string _conversationId;
        private readonly string _token;
        private string _watermark;

        public TestBotClient(IBotTestConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (string.IsNullOrEmpty(config.DirectLineSecret))
            {
                throw new ArgumentNullException(nameof(config.DirectLineSecret));
            }

            if (string.IsNullOrEmpty(config.BotId))
            {
                throw new ArgumentNullException(nameof(config.BotId));
            }

            // Instead of generating a vanilla DirectLineClient with secret, 
            // we obtain a directLine token with the secrets and then we use
            // that token to create the directLine client.
            // What this gives us is the ability to pass TrustedOrigins when obtaining the token,
            // which tests the enhanced authentication.
            // This endpoint is unfortunately not supported by the directLine client which is 
            // why we add this custom code.
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"https://directline.botframework.com/v3/directline/tokens/generate");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.DirectLineSecret);
                request.Content = new StringContent(JsonConvert.SerializeObject(new
                {
                    User = new { Id = _user },
                    TrustedOrigins = new string[]
                        {
                            OriginHeaderValue
                        }
                }), Encoding.UTF8, "application/json");

                using (var response = client.SendAsync(request).GetAwaiter().GetResult())
                {
                    if (response.IsSuccessStatusCode)
                    {
                        // Extract token from response
                        var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        _token = JsonConvert.DeserializeObject<DirectLineToken>(body).Token;
                        _conversationId = JsonConvert.DeserializeObject<DirectLineToken>(body).ConversationId;

                        // Create directLine client from token
                        _directLineClient = new DirectLineClient(_token);

                        // From now on, we'll add an Origin header in directLine calls, with 
                        // the trusted origin we sent when acquiring the token as value.
                        _directLineClient.HttpClient.DefaultRequestHeaders.Add(OriginHeaderKey, OriginHeaderValue);
                    }
                    else
                    {
                        throw new Exception("Failed to acquire directLine token");
                    }
                }
            }
        }

        public Task<ResourceResponse> SendMessageAsync(string message, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(nameof(message)))
            {
                throw new ArgumentNullException(nameof(message));
            }

            // Create a message activity with the input text.
            var messageActivity = new Activity
            {
                From = new ChannelAccount(_user),
                Text = message,
                Type = ActivityTypes.Message,
            };

            Console.WriteLine($"Sent to bot: {message}");
            return SendActivityAsync(messageActivity, cancellationToken);
        }

        public async Task<ResourceResponse[]> SendMessagesAsync(IEnumerable<string> messages, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (messages == null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            var resourceResponses = new List<ResourceResponse>();

            foreach (var message in messages)
            {
                resourceResponses.Add(await SendMessageAsync(message, cancellationToken));
            }

            return resourceResponses.ToArray();
        }

        public async Task StartConversation(CancellationToken cancellationToken = default(CancellationToken))
        {
            var conversation = await _directLineClient.Conversations.StartConversationAsync(cancellationToken);
            _conversationId = conversation?.ConversationId ?? throw new InvalidOperationException("Conversation cannot be null");
        }

        public Task<ResourceResponse> SendActivityAsync(Activity activity, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Send the message activity to the bot.
            return _directLineClient.Conversations.PostActivityAsync(_conversationId, activity, cancellationToken);
        }

        public async Task AssertReplyAsync(string expected, CancellationToken cancellationToken = default(CancellationToken))
        {
            var messages = await PollBotMessagesAsync(cancellationToken);
            Console.WriteLine("Messages sent from bot:");
            var messagesList = messages.ToList();
            foreach (var m in messagesList.ToList())
            {
                Console.WriteLine($"Type:{m.Type}; Text:{m.Text}");
            }
            Assert.True(messagesList.Any(m => m.Type == ActivityTypes.Message && m.Text.Contains(expected, StringComparison.OrdinalIgnoreCase)), $"Expected: {expected}");
        }

        public async Task AssertReplyOneOf(IEnumerable<string> expected, CancellationToken cancellationToken = default(CancellationToken))
        {
            var messages = await PollBotMessagesAsync(cancellationToken);
            Assert.Contains(messages, m => m.Type == ActivityTypes.Message && expected.Any(e => m.Text.Contains(e, StringComparison.OrdinalIgnoreCase)));
        }

        public async Task<IEnumerable<Activity>> PollBotMessagesAsync(CancellationToken cancellationToken = default)
        {
            // Even if we receive a cancellation token with a super long timeout,
            // we set a cap on the max time this while loop can run
            var maxCancellation = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            while (!cancellationToken.IsCancellationRequested && !maxCancellation.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
                
                var activities = await ReadBotMessagesAsync(cancellationToken);

                if (activities != null && activities.Any())
                {
                    return activities;
                }
            }

            throw new Exception("No activities received");
        }

        public async Task<IEnumerable<Activity>> ReadBotMessagesAsync(CancellationToken cancellationToken = default)
        {
            // Retrieve activities from directLine
            var activitySet = await _directLineClient.Conversations.GetActivitiesAsync(_conversationId, _watermark, cancellationToken);
            _watermark = activitySet?.Watermark;

            // Extract and return the activities sent from the bot.
            return activitySet?.Activities?.Where(activity => activity.From.Id == _config.BotId);
        }

        public async Task SignInAndVerifyOAuthAsync(Activity oAuthCard, CancellationToken cancellationToken = default)
        {
            // We obtained what we think is an OAuthCard. Steps to follow:
            // 1- Verify we have a sign in link
            // 2- Get directLine session id and cookie
            // 3- Follow sign in link but manually do each redirect
            //      3.a- Detect the PostSignIn url in the redirect chain 
            //      3.b- Add cookie and challenge session id to post sign in link
            // 4- Verify final redirect to token service ends up in success

            // 1- Verify we have a sign in link in the activity

            if (oAuthCard == null)
            {
                throw new Exception("OAuthCard is null");
            }
            else if (oAuthCard.Attachments == null)
            {
                throw new Exception("OAuthCard.Attachments = null");
            }

            var card = JsonConvert.DeserializeObject<SigninCard>(JsonConvert.SerializeObject(oAuthCard.Attachments.FirstOrDefault().Content));

            if (card == null)
            {
                throw new Exception("No SignIn Card received in activity");
            }

            if (card.Buttons == null || !card.Buttons.Any())
            {
                throw new Exception("No buttons received in sign in card");
            }

            var signInUrl = card.Buttons[0].Value?.ToString();

            if (string.IsNullOrEmpty(signInUrl) || !signInUrl.StartsWith("https://"))
            {
                throw new Exception($"Sign in url is empty or badly formatted. Url received: {signInUrl}");
            }

            // 2- Get directLine session id and cookie
            var sessionInfo = await GetSessionInfoAsync();

            // 3- Follow sign in link but manually do each redirect
            // 4- Verify final redirect to token service ends up in success
            await SignInAsync(sessionInfo, signInUrl);
        }

        private async Task SignInAsync(DirectLineSessionInfo directLineSession, string url)
        {
            var cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler()
            {
                AllowAutoRedirect = false,
                CookieContainer = cookieContainer
            };

            // We have a sign in url, which will produce multiple HTTP 302 for redirects
            // This will path 
            //      token service -> other services -> auth provider -> token service (post sign in)-> response with token
            // When we receive the post sign in redirect, we add the cookie passed in the directLine session info
            // to test enhanced authentication. This in ther scenarios happens by itself since browsers do this for us.
            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add(OriginHeaderKey, OriginHeaderValue);

                while (!string.IsNullOrEmpty(url))
                {
                    using (var response = await client.GetAsync(url))
                    {
                        var text = await response.Content.ReadAsStringAsync();

                        url = response.StatusCode == HttpStatusCode.Redirect
                            ? response.Headers.Location.OriginalString
                            : null;

                        // Once the redirects are done, there is no more url. This means we 
                        // did the entire loop
                        if (url == null)
                        {
                            Assert.True(response.IsSuccessStatusCode);
                            Assert.Contains("You are now signed in and can close this window.", text);
                            return;
                        }

                        // If this is the post sign in callback, add the cookie and code challenge
                        // so that the token service gets the verification.
                        // Here we are simulating what Webchat does along with the browser cookies.
                        if (url.StartsWith("https://token.botframework.com/api/oauth/PostSignInCallback"))
                        {
                            url += $"&code_challenge={ directLineSession.SessionId }";
                            cookieContainer.Add(directLineSession.Cookie);
                        }
                    }
                }

                throw new Exception("Sign in did not succeed. Set a breakpoint in TestBotClient.SignInAsync() to debug the redirect sequence.");
            }
        }

        private async Task<DirectLineSessionInfo> GetSessionInfoAsync()
        {
            // Set up cookie container to obtain response cookie
            var cookies = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = cookies
            };

            using (var client = new HttpClient(handler))
            {
                // Call the directLine session api, not supported by DirectLine client
                const string getSessionUrl = "https://directline.botframework.com/v3/directline/session/getsessionid";
                var request = new HttpRequestMessage(HttpMethod.Get, getSessionUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

                // We want to add the Origins header to this client as well
                client.DefaultRequestHeaders.Add(OriginHeaderKey, OriginHeaderValue);


                using (var response = await client.SendAsync(request))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        // The directLine response that is relevant to us is the cookie and the session info.

                        // Extract cookie from cookies
                        var cookie = cookies.GetCookies(new Uri(getSessionUrl)).Cast<Cookie>().FirstOrDefault(c => c.Name == "webchat_session_v2");

                        // Extract session info from body
                        var body = await response.Content.ReadAsStringAsync();
                        var session = JsonConvert.DeserializeObject<DirectLineSession>(body);

                        return new DirectLineSessionInfo()
                        {
                            SessionId = session.SessionId,
                            Cookie = cookie
                        };
                    }

                    throw new Exception("Failed to obtain session id");
                }
            }
        }
    }

    public class DirectLineToken
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("conversationId")]
        public string ConversationId { get; set; }
    }

    public class DirectLineSession
    {
        [JsonProperty("sessionId")]
        public string SessionId { get; set; }
    }

    public class DirectLineSessionInfo
    {
        public string SessionId { get; set; }
        public Cookie Cookie { get; set; }
    }
}
