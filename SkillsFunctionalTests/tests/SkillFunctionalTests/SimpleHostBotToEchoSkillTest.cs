// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Threading.Tasks;
using TranscriptTestRunner;
using Xunit;

namespace SkillFunctionalTests
{
    [Trait("TestCategory", "FunctionalTests")]
    public class SimpleHostBotToEchoSkillTest
    {
        private readonly string _transcriptsFolder = Directory.GetCurrentDirectory() + @"/SourceTranscripts";

        [Fact]
        public async Task HostWhenRequestedShouldRedirectToSkill()
        {
            var runner = new TestRunner(new TestClientFactory(ClientType.DirectLine).GetTestClient());

            await runner.RunTestAsync($"{_transcriptsFolder}/ShouldRedirectToSkill.transcript").ConfigureAwait(false);
        }

        [Fact]
        public async Task HostWhenSkillEndsHostReceivesEndOfConversation()
        {
            var runner = new TestRunner(new TestClientFactory(ClientType.DirectLine).GetTestClient());

            await runner.RunTestAsync($"{_transcriptsFolder}/HostReceivesEndOfConversation.transcript").ConfigureAwait(false);
        }

        [Fact]
        public async Task HostWhenRequestedShouldRunTestTranscript()
        {
            await TestRunner.RunTestAsync(
                ClientType.DirectLine,
                $"{_transcriptsFolder}/ShouldRedirectToSkill.transcript",
                $"{_transcriptsFolder}/HostReceivesEndOfConversation.transcript").ConfigureAwait(false);
        }
    }
}
