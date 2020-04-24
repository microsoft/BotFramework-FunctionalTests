using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkillFunctionalTests.Bot;
using SkillFunctionalTests.Configuration;

namespace FunctionalTests
{
    [TestClass]
    [TestCategory("FunctionalTests")]
    public class SimpleHostBotToEchoSkillTest
    {
        [TestMethod]
        public async Task Host_WhenRequested_ShouldRedirectToSkill()
        {
            // If the test takes more than one minute, declare failure.
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var testBot = new TestBotClient(new EnvironmentBotTestConfiguration());

            await testBot.StartConversation(cancellationTokenSource.Token);
            await testBot.SendMessageAsync("Hi", cancellationTokenSource.Token);
            await testBot.AssertReplyAsync("Me no nothin", cancellationTokenSource.Token);
            await testBot.SendMessageAsync("skill", cancellationTokenSource.Token);
            await testBot.AssertReplyAsync("Echo: skill", cancellationTokenSource.Token);
        }

        [TestMethod]
        public async Task Host_WhenSkillEnds_HostReceivesEndOfConversation()
        {
            // If the test takes more than one minute, declare failure.
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var testBot = new TestBotClient(new EnvironmentBotTestConfiguration());

            await testBot.StartConversation(cancellationTokenSource.Token);
            await testBot.SendMessageAsync("Hi", cancellationTokenSource.Token);
            await testBot.AssertReplyAsync("Me no nothin", cancellationTokenSource.Token);
            await testBot.SendMessageAsync("skill", cancellationTokenSource.Token);
            await testBot.AssertReplyAsync("Echo: skill", cancellationTokenSource.Token);
            await testBot.SendMessageAsync("end", cancellationTokenSource.Token);
            await testBot.AssertReplyAsync("Received endOfConversation", cancellationTokenSource.Token);
        }
    }
}
