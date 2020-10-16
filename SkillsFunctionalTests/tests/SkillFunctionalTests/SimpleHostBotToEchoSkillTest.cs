// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SkillFunctionalTests.Bot;
using SkillFunctionalTests.Configuration;
using TranscriptTestRunner;
using Xunit;

namespace FunctionalTests
{
    [Trait("TestCategory", "FunctionalTests")]
    public class SimpleHostBotToEchoSkillTest
    {
        private readonly string _transcriptsFolder = Directory.GetCurrentDirectory() + @"/SourceTranscripts";

        [Fact]
        public async Task Host_WhenRequested_ShouldRedirectToSkill()
        {
            var runner = new TestRunner(new TestClientFactory(ClientType.DirectLine).GetTestClient());

            await runner.RunTestAsync($"{ _transcriptsFolder }/ShouldRedirectToSkill.transcript");
        }

        [Fact]
        public async Task Host_WhenSkillEnds_HostReceivesEndOfConversation()
        {
            var runner = new TestRunner(new TestClientFactory(ClientType.DirectLine).GetTestClient());

            await runner.RunTestAsync($"{ _transcriptsFolder }/HostReceivesEndOfConversation.transcript");
        }

        [Fact]
        public async Task Host_WhenRequested_ShouldRunTestTranscript()
        {
            await TestRunner.RunTestAsync(
                ClientType.DirectLine,
                $"{ _transcriptsFolder }/ShouldRedirectToSkill.transcript",
                $"{ _transcriptsFolder }/HostReceivesEndOfConversation.transcript");
        }
    }
}
