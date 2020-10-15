// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.Extensions.Configuration;
using BotActivity = Microsoft.Bot.Schema.Activity;

namespace TranscriptTestRunner.TestClients
{
    public class DirectLineTestClient : TestClientBase
    {
        private DirectLineClient Client { get; }

        private const string DirectLineSecretKey = "DIRECTLINE";
        private const string BotIdKey = "BOTID";
        
        private readonly string _user = $"TestUser-{Guid.NewGuid()}";
        private string _conversationId;
        private static string _directLineSecret;
        private static string _botId;
        
        public DirectLineTestClient(IConfiguration config)
        {
            GetConfiguration(config);

            Client = new DirectLineClient(_directLineSecret);
        }

        public override async Task SendActivityAsync(BotActivity activity)
        {
            if (string.IsNullOrWhiteSpace(_conversationId))
            {
                await CreateConversationAsync();
            }

            var activityPost = new Activity
            {
                From = new ChannelAccount(_user),
                Text = activity.Text,
                Type = activity.Type
            };

            await Client.Conversations.PostActivityAsync(_conversationId, activityPost, default);
        }

        public override async Task<bool> ValidateActivityAsync(BotActivity expected)
        {
            var activities = await PollBotMessagesAsync(default);
            var botMessages = activities.Where(a => a.Type == ActivityTypes.Message);

            return botMessages.Any(message => message.Text == expected.Text);
        }

        private static void GetConfiguration(IConfiguration config)
        {
            _directLineSecret = config[DirectLineSecretKey];

            if (string.IsNullOrWhiteSpace(_directLineSecret))
            {
                throw new ArgumentException($"Configuration setting '{DirectLineSecretKey}' not found.");
            }

            _botId = config[BotIdKey];
            
            if (string.IsNullOrWhiteSpace(_botId))
            {
                throw new ArgumentException($"Configuration setting '{BotIdKey}' not set.");
            }
        }

        private async Task<IEnumerable<Activity>> PollBotMessagesAsync(CancellationToken cancellationToken = default)
        {
            var maxCancellation = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            while (!cancellationToken.IsCancellationRequested && !maxCancellation.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);

                var activities = await ReadBotMessagesAsync(cancellationToken);

                if (activities != null && activities.Any())
                {
                    return activities;
                }
            }

            throw new Exception("No activities received");
        }

        private async Task<IEnumerable<Activity>> ReadBotMessagesAsync(CancellationToken cancellationToken = default)
        {
            var activitySet = await Client.Conversations.GetActivitiesAsync(_conversationId, null, cancellationToken);

            // Extract and return the activities sent from the bot.
            return activitySet?.Activities?.Where(activity => activity.From.Id == _botId);
        }

        private async Task CreateConversationAsync()
        {
            var conversation = await Client.Conversations.StartConversationAsync();

            _conversationId = conversation.ConversationId;
        }
    }
}
