using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SkillFunctionalTests.Bot;
using SkillFunctionalTests.Configuration;

namespace FunctionalTests
{
    [TestClass]
    [TestCategory("FunctionalTests")]
    public class OAuthSkillTest
    {
        [TestMethod]
        public async Task OAuthCalledInSkill()
        {
            // If the test takes more than one minute, declare failure.
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));

            var botChecker = new TestBotClient(new EnvironmentBotTestConfiguration());

            await botChecker.StartConversation(cancellationTokenSource.Token);
            await botChecker.SendMessageAsync("Hello", cancellationTokenSource.Token);
            await botChecker.AssertReplyAsync("Me no nothin", cancellationTokenSource.Token);
            await botChecker.SendMessageAsync("skill", cancellationTokenSource.Token);
            await botChecker.AssertReplyAsync("Echo: skill", cancellationTokenSource.Token);
            await botChecker.SendMessageAsync("auth", cancellationTokenSource.Token);
            var messages = await botChecker.ReadBotMessagesAsync(cancellationTokenSource.Token);
            await botChecker.SignInAndVerifyOAuthAsync(messages.FirstOrDefault(m => m.Attachments != null && m.Attachments.Any()), cancellationTokenSource.Token);
        }
    }
}
