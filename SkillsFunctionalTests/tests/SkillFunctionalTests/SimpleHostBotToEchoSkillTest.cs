// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using SkillFunctionalTests.Bot;
using SkillFunctionalTests.Configuration;
using Xunit;

namespace FunctionalTests
{
    [Trait("TestCategory", "FunctionalTests")]
    public class SimpleHostBotToEchoSkillTest
    {
        [Fact]
        public async Task Host_WhenRequested_ShouldRedirectToSkill()
        {
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            var testBot = new TestBotClient(new EnvironmentBotTestConfiguration());

            await testBot.StartConversation(cancellationTokenSource.Token);
            await testBot.SendMessageAsync("Hi", cancellationTokenSource.Token);
            await testBot.AssertReplyAsync("Me no nothin", cancellationTokenSource.Token);
            await testBot.SendMessageAsync("skill", cancellationTokenSource.Token);
            await testBot.AssertReplyAsync("Echo: skill", cancellationTokenSource.Token);
        }

        [Fact]
        public async Task Host_WhenSkillEnds_HostReceivesEndOfConversation()
        {
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

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
