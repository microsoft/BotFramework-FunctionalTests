// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TranscriptTestRunner;
using Xunit;
using Xunit.Abstractions;

namespace SkillFunctionalTests
{
    [Trait("TestCategory", "FunctionalTests")]
    public class SimpleHostBotToEchoSkillTest
    {
        private readonly string _transcriptsFolder = Directory.GetCurrentDirectory() + @"/SourceTranscripts";
        private readonly ILogger<SimpleHostBotToEchoSkillTest> _logger;

        public SimpleHostBotToEchoSkillTest(ITestOutputHelper output)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Trace)
                    .AddConsole()
                    .AddDebug()
                    .AddXunit(output);
            });

            _logger = loggerFactory.CreateLogger<SimpleHostBotToEchoSkillTest>();
        }

        [Theory]
        [InlineData("ShouldRedirectToSkill.transcript")]
        [InlineData("HostReceivesEndOfConversation.transcript")]
        public async Task RunScripts(string transcript)
        {
            var runner = new TestRunner(new TestClientFactory(ClientType.DirectLine).GetTestClient(), _logger);
            await runner.RunTestAsync(Path.Combine(_transcriptsFolder, transcript));
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
