// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TranscriptTestRunner;
using Xunit;

namespace SkillFunctionalTests
{
    [Trait("TestCategory", "FunctionalTests")]
    public class SimpleHostBotToEchoSkillTest
    {
        private readonly string _transcriptsFolder = Directory.GetCurrentDirectory() + @"/SourceTranscripts";
        private readonly ILogger<SimpleHostBotToEchoSkillTest> _logger;

        public SimpleHostBotToEchoSkillTest()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Trace)
                    .AddConsole()
                    .AddDebug();
            });

            _logger = loggerFactory.CreateLogger<SimpleHostBotToEchoSkillTest>();
        }

        [Fact]
        public async Task HostWhenRequestedShouldRedirectToSkill()
        {
            var runner = new TestRunner(new TestClientFactory(ClientType.DirectLine).GetTestClient(), _logger);

            await runner.RunTestAsync($"{_transcriptsFolder}/ShouldRedirectToSkill.transcript").ConfigureAwait(false);
        }

        [Fact]
        public async Task HostWhenSkillEndsHostReceivesEndOfConversation()
        {
            var runner = new TestRunner(new TestClientFactory(ClientType.DirectLine).GetTestClient(), _logger);

            await runner.RunTestAsync($"{_transcriptsFolder}/HostReceivesEndOfConversation.transcript").ConfigureAwait(false);
        }

        [Fact]
        public async Task HostWhenRequestedShouldRunTestTranscript()
        {
            await TestRunner.RunTestAsync(
                ClientType.DirectLine, 
                _logger,
                $"{_transcriptsFolder}/ShouldRedirectToSkill.transcript",
                $"{_transcriptsFolder}/HostReceivesEndOfConversation.transcript").ConfigureAwait(false);
        }
    }
}
