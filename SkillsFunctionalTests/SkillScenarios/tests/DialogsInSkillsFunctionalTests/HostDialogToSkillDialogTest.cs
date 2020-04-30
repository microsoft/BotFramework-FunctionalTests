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
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly TestBotClient testBot;

        public HostDialogToSkillDialogTest()
        {
            cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            testBot = new TestBotClient(new EnvironmentBotTestConfiguration());
        }

        [Fact]
        public async Task BothHostAndSkill_CanRunTheirOwnDialogs()
        {
            await testBot.StartConversation(cancellationTokenSource.Token);
            await RunEchoSkillAsync(testBot, cancellationTokenSource.Token);
            await BookFlightWithDialogSkillAsync(testBot, cancellationTokenSource.Token);
        }

        [Fact]
        public async Task Bot_CanBeBoth_SkillAndHost()
        {
            await testBot.StartConversation(cancellationTokenSource.Token);
            await RunEchoSkillFromDialogSkill(testBot, cancellationTokenSource.Token);
        }

        private async Task RunEchoSkillAsync(TestBotClient testBot, CancellationToken cancellationToken)
        {
            await testBot.AssertReplyAsync("What skill would you like to call?", cancellationToken);
            await testBot.SendMessageAsync("EchoSkillBot", cancellationToken);
            var selectEchoActionText = "Select an action # to send to **EchoSkillBot** or just type in a message and it will be forwarded to the skill";
            await testBot.AssertReplyAsync(selectEchoActionText, cancellationToken);
            await testBot.SendMessageAsync("Message", cancellationToken);
            await testBot.AssertReplyAsync("Echo (dotnet) : Message", cancellationToken);
            await testBot.SendMessageAsync("end", cancellationToken);
            await testBot.AssertReplyAsync("Done with \"EchoSkillBot\". \n\n What skill would you like to call?", cancellationToken);
        }

        private async Task BookFlightWithDialogSkillAsync(TestBotClient testBot, CancellationToken cancellationToken)
        {
            await SelectDialogSkillAsync(testBot, cancellationToken);
            await testBot.SendMessageAsync("BookFlight with input parameters", cancellationToken);
            await testBot.AssertReplyAsync("When would you like to travel?", cancellationToken);
            await testBot.SendMessageAsync("tomorrow", cancellationToken);
            await testBot.AssertReplyAsync("Please confirm, I have you traveling to: Seattle from: New York", cancellationToken);
            await testBot.SendMessageAsync("Yes", cancellationToken);
            await testBot.AssertReplyAsync("Done with \"DialogSkillBot\". \n\n What skill would you like to call?", cancellationToken);
        }

        private async Task RunEchoSkillFromDialogSkill(TestBotClient testBot, CancellationToken cancellationToken)
        {
            await testBot.AssertReplyAsync("What skill would you like to call?", cancellationToken);
            await SelectDialogSkillAsync(testBot, cancellationToken);
            await testBot.SendMessageAsync("EchoSkill", cancellationToken);
            await testBot.AssertReplyAsync("Echo (dotnet) : Hello echo", cancellationToken);
            await testBot.SendMessageAsync("2nd message to echo skill", cancellationToken);
            await testBot.AssertReplyAsync("Echo (dotnet) : 2nd message to echo skill", cancellationToken);
            await testBot.SendMessageAsync("end", cancellationToken);
            await testBot.AssertReplyAsync("Done with \"DialogSkillBot\". \n\n What skill would you like to call?", cancellationToken);
        }

        private async Task SelectDialogSkillAsync(TestBotClient testBot, CancellationToken cancellationToken)
        {
            await testBot.SendMessageAsync("DialogSkillBot", cancellationToken);
            var selectDialogActionText = "Select an action # to send to **DialogSkillBot** or just type in a message and it will be forwarded to the skill\n\n   1. BookFlight\n   2. BookFlight with input parameters\n   3. GetWeather\n   4. EchoSkill";
            await testBot.AssertReplyAsync(selectDialogActionText, cancellationToken);
        }
    }
}
