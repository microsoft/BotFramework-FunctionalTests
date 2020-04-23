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
        public async Task ShouldReceiveSkillAnswerAsync()
        {
            // If the test takes more than one minute, declare failure.
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var testBot = new TestBotClient(new EnvironmentBotTestConfiguration());

            var msg = $"Testing skill bot, GUID: { Guid.NewGuid() }";

            await testBot.SendMessageAsync(msg);
            await testBot.AssertReplyAsync($"Echo: { msg }");
            await testBot.SendMessageAsync("end");
        }
    }
}
