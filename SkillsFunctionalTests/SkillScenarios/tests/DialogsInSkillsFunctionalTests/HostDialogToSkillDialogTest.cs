using Microsoft.Bot.Connector.DirectLine;
using SkillFunctionalTests.Bot;
using SkillFunctionalTests.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FunctionalTests.SkillScenarios
{
    public class HostDialogToSkillDialogTest
    {
        [Fact]
        public async Task BothHostAndSkill_CanRunTheirOwnDialogs()
        {
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));

            var testBot = new TestBotClient(new EnvironmentBotTestConfiguration());

            await testBot.StartConversation(cancellationTokenSource.Token);
            await RunEchoSkillAsync(testBot, cancellationTokenSource.Token);
            await RunDialogSkillAsync(testBot, cancellationTokenSource.Token);
        }

        [Fact]
        public async Task Bot_CanBeBoth_SkillAndHost()
        {
            
        }

        private async Task RunEchoSkillAsync(TestBotClient testBot, CancellationToken cancellationToken)
        {
            // TODO: need to update TestBotClient to handle Message activities that have null Text 
                // like a welcome message using an adaptive card attachment
                // Currently throws NullReferenceError -- temporarily commented out the welcome message in the Root bot for now
            // await testBot.AssertReplyAsync(null, cancellationToken);
            await testBot.AssertReplyAsync("What skill would you like to call?", cancellationToken);
            await testBot.SendMessageAsync("EchoSkillBot", cancellationToken);
            var selectEchoActionText = "Select an action # to send to **EchoSkillBot** or just type in a message and it will be forwarded to the skill";
            await testBot.AssertReplyAsync(selectEchoActionText, cancellationToken);
            await testBot.SendMessageAsync("Message", cancellationToken);
            await testBot.AssertReplyAsync("Echo (dotnet) : Message", cancellationToken);

            // TODO: update TestBotClient to assert reply that contains multiple messages
            // await testBot.AssertReplyAsync("Say “end” or “stop” and I’ll end the conversation and back to the parent.", cancellationToken);

            await testBot.SendMessageAsync("end", cancellationToken);
            // Skipping following message check
            //await testBot.AssertReplyAsync("ending conversation from the skill...", cancellationToken);
            await testBot.AssertReplyAsync("Done with \"EchoSkillBot\". \n\n What skill would you like to call?", cancellationToken);
        }

        private async Task RunDialogSkillAsync(TestBotClient testBot, CancellationToken cancellationToken)
        {
            await testBot.SendMessageAsync("DialogSkillBot", cancellationToken);
            var selectDialogActionText = "Select an action # to send to **DialogSkillBot** or just type in a message and it will be forwarded to the skill\n\n   1. BookFlight\n   2. BookFlight with input parameters\n   3. GetWeather\n   4. EchoSkill";
            await testBot.AssertReplyAsync(selectDialogActionText, cancellationToken);
            await testBot.SendMessageAsync("BookFlight with input parameters", cancellationToken);
            await testBot.AssertReplyAsync("When would you like to travel?", cancellationToken);
            await testBot.SendMessageAsync("tomorrow", cancellationToken);
            await testBot.AssertReplyAsync("Please confirm, I have you traveling to: Seattle from: New York", cancellationToken);
            await testBot.SendMessageAsync("Yes", cancellationToken);
            await testBot.AssertReplyAsync("Done with \"DialogSkillBot\". \n\n What skill would you like to call?", cancellationToken);
        }
    }
}
