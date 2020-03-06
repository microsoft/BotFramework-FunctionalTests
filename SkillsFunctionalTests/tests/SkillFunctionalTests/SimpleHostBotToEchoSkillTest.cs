using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SkillFunctionalTests
{
    [TestClass]
    [TestCategory("FunctionalTests")]
    public class SimpleHostBotToEchoSkillTest
    {
        private static string _directLineSecret = "";
        private static string _botId = "";
        private static string _input = "Testing skill bot, GUID: ";
        private static readonly List<string> Messages = new List<string>();
        private const string User = "DirectLineClientTestUser";
        private const string EndUtterance = "end";

        [TestMethod]
        public async Task ShouldReceiveSkillAnswerAsync()
        {            
            var echoGuid = Guid.NewGuid().ToString();
            _input += echoGuid;
            Messages.Add(_input);
            Messages.Add(EndUtterance);

            GetEnvironmentVars();

            var botAnswer = await SendMessagesToBotAsync(User, Messages);

            Assert.AreEqual($"Echo: {_input}", botAnswer);
        }

        /// <summary>
        /// Starts a conversation with a bot. Sends a message and waits for the response.
        /// </summary>
        /// <returns>Returns the bot's answer.</returns>
        private static async Task<string> SendMessagesToBotAsync(string user, List<string> messages)
        {
            // Create a new Direct Line client.
            var client = new DirectLineClient(_directLineSecret);

            // Start the conversation.
            var conversation = await client.Conversations.StartConversationAsync();

            // Send messages to the conversation.
            foreach (var message in messages)
            {
                // Create a message activity with the input text.
                var userMessage = new Activity
                {
                    From = new ChannelAccount(user),
                    Text = message,
                    Type = ActivityTypes.Message,
                };

                // Send the message activity to the bot.
                await client.Conversations.PostActivityAsync(conversation.ConversationId, userMessage);
            } 

            // Read the bot's message.
            var botAnswer = await ReadBotMessagesAsync(client, conversation.ConversationId);

            return botAnswer;
        }

        /// <summary>
        /// Polls the bot continuously until it gets a response.
        /// </summary>
        /// <param name="client">The Direct Line client.</param>
        /// <param name="conversationId">The conversation ID.</param>
        /// <returns>Returns the bot's answer.</returns>
        private static async Task<string> ReadBotMessagesAsync(DirectLineClient client, string conversationId)
        {
            string watermark = null;
            var answer = string.Empty;

            // Poll the bot for replies once per second.
            while (answer.Equals(string.Empty))
            {
                // Retrieve the activity sent from the bot.
                var activitySet = await client.Conversations.GetActivitiesAsync(conversationId, watermark);
                watermark = activitySet?.Watermark;

                // Extract the activities sent from the bot.
                var activities = from x in activitySet.Activities
                                 where x.From.Id == _botId
                                 select x;

                // Select the message that matches with Echo
                answer = activities
                    .Where(activity => activity.Text.Contains("Echo")).FirstOrDefault().Text;

                // Wait for one second before polling the bot again.
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

                return answer;
            }

            return answer;
        }

        /// <summary>
        /// Get the values for the environment variables.
        /// </summary>
        private static void GetEnvironmentVars()
        {
            if (string.IsNullOrWhiteSpace(_directLineSecret) || string.IsNullOrWhiteSpace(_botId))
            {
                _directLineSecret = Environment.GetEnvironmentVariable("DIRECTLINE");
                if (string.IsNullOrWhiteSpace(_directLineSecret))
                {
                    Assert.Inconclusive("Environment variable 'DIRECTLINE' not found.");
                }

                _botId = Environment.GetEnvironmentVariable("BOTID");
                if (string.IsNullOrWhiteSpace(_botId))
                {
                    Assert.Inconclusive("Environment variable 'BOTID' not found.");
                }
            }
        }
    }
}
