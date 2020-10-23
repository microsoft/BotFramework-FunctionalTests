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
using Xunit.Sdk;

namespace SkillFunctionalTests
{
    [Trait("TestCategory", "FunctionalTests")]
    [Trait("TestCategory", "OAuth")]
    [Trait("TestCategory", "SkipForV3Bots")]
    public class OAuthSkillTest
    {
        [Fact]
        public async Task SkillOAuthCardSignInSuccessful()
        {
            // If the test takes more than two minutes, declare failure.
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            var testBot = new TestBotClient(new BotTestConfiguration());

            await testBot.StartConversation(cancellationTokenSource.Token).ConfigureAwait(false);
            await testBot.SendMessageAsync("Hello", cancellationTokenSource.Token).ConfigureAwait(false);
            await testBot.AssertReplyAsync("Me no nothin", cancellationTokenSource.Token).ConfigureAwait(false);
            await testBot.SendMessageAsync("skill", cancellationTokenSource.Token).ConfigureAwait(false);
            await testBot.AssertReplyAsync("Echo: skill", cancellationTokenSource.Token).ConfigureAwait(false);
            await testBot.SendMessageAsync("auth", cancellationTokenSource.Token).ConfigureAwait(false);
            var messages = await testBot.ReadBotMessagesAsync(cancellationTokenSource.Token).ConfigureAwait(false);

            var activities = messages.ToList();

            Console.WriteLine("Enumerating activities:");
            
            foreach (var a in activities)
            {
                Console.WriteLine($"Type={a.Type}; Text={a.Text}; Code={a.Code}; Attachments count={a.Attachments.Count}");
            }

            var error = activities.FirstOrDefault(
                m => m.Type == ActivityTypes.EndOfConversation && m.Code == "SkillError");
            
            if (error != null)
            {
                throw new XunitException(error.Text);
            }

            await testBot.SignInAndVerifyOAuthAsync(activities.FirstOrDefault(m => m.Attachments != null && m.Attachments.Any()), cancellationTokenSource.Token).ConfigureAwait(false);
        }
    }
}
