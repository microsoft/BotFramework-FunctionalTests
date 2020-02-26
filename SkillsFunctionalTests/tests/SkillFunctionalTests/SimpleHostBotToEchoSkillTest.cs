using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunctionalTests
{
    [TestClass]
    [TestCategory("FunctionalTests")]
    public class SimpleHostBotToEchoSkillTest
    {
        private static string directLineSecret = "";
        private static string botId = "";
        private static List<string> messages = new List<string>();
        private static string user = "DirectLineClientTestUser";
        private static string input = "Testing skill bot, GUID: ";
        private static string endUtterance = "end";

        [TestMethod]
        public async Task ShouldReceiveDotNetSkillAnswer()
        {            
            string echoGuid = string.Empty;

            echoGuid = Guid.NewGuid().ToString();
            input += echoGuid;
            messages.Add(input);
            messages.Add(endUtterance);

            GetEnvironmentVars();

            var botAnswer = await SendMessagesToBot(user, messages);

            Assert.AreEqual($"Echo: {input}", botAnswer);
        }

        /// <summary>
        /// Starts a conversation with a bot. Sends a message and waits for the response.
        /// </summary>
        /// <returns>Returns the bot's answer.</returns>
        private static async Task<string> SendMessagesToBot(string user, List<string> messages)
        {
            // Create a new Direct Line client.
            var client = new DirectLineClient(directLineSecret);

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
                                 where x.From.Id == botId
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
        private void GetEnvironmentVars()
        {
            if (string.IsNullOrWhiteSpace(directLineSecret) || string.IsNullOrWhiteSpace(botId))
            {
                directLineSecret = Environment.GetEnvironmentVariable("DIRECTLINE");
                if (string.IsNullOrWhiteSpace(directLineSecret))
                {
                    Assert.Inconclusive("Environment variable 'DIRECTLINE' not found.");
                }

                botId = Environment.GetEnvironmentVariable("BOTID");
                if (string.IsNullOrWhiteSpace(botId))
                {
                    Assert.Inconclusive("Environment variable 'BOTID' not found.");
                }
            }
        }
    }
}
