// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
        public async Task Skill_OAuthCard_SignInSuccessful()
        {
            // If the test takes more than one minute, declare failure.
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            var testBot = new TestBotClient(new EnvironmentBotTestConfiguration());

            await testBot.StartConversation(cancellationTokenSource.Token);
            await testBot.SendMessageAsync("Hello", cancellationTokenSource.Token);
            await testBot.AssertReplyAsync("Me no nothin", cancellationTokenSource.Token);
            await testBot.SendMessageAsync("skill", cancellationTokenSource.Token);
            await testBot.AssertReplyAsync("Echo: skill", cancellationTokenSource.Token);
            await testBot.SendMessageAsync("auth", cancellationTokenSource.Token);
            var messages = await testBot.ReadBotMessagesAsync(cancellationTokenSource.Token);

            var activities = messages.ToList();
            var error = activities.FirstOrDefault(
                m => m.Type == ActivityTypes.EndOfConversation && m.Code == "SkillError");
            
            if (error != null)
            {
                Assert.Fail(error.Text);
            }

            await testBot.SignInAndVerifyOAuthAsync(activities.FirstOrDefault(m => m.Attachments != null && m.Attachments.Any()), cancellationTokenSource.Token);
        }
    }
}
