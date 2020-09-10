// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.DirectLine;
using SkillFunctionalTests.Bot;
using SkillFunctionalTests.Configuration;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace FunctionalTests
{
    [Trait("TestCategory", "FunctionalTests")]
    [Trait("TestCategory", "OAuth")]
    [Trait("TestCategory", "SkipForV3Bots")]
    public class OAuthSkillTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public OAuthSkillTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Skill_OAuthCard_SignInSuccessful()
        {
            // If the test takes more than two minutes, declare failure.
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

            _testOutputHelper.WriteLine("Enumerating activities:");
            
            foreach (var a in activities)
            {
                _testOutputHelper.WriteLine($"Type={a.Type}; Text={a.Text}; Code={a.Code}; Attachments count={a.Attachments.Count}");
            }

            var error = activities.FirstOrDefault(
                m => m.Type == ActivityTypes.EndOfConversation && m.Code == "SkillError");
            
            if (error != null)
            {
                throw new XunitException(error.Text);
            }

            await testBot.SignInAndVerifyOAuthAsync(activities.FirstOrDefault(m => m.Attachments != null && m.Attachments.Any()), cancellationTokenSource.Token);
        }
    }
}
