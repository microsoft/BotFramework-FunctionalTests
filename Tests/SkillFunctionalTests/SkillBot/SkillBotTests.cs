// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TranscriptTestRunner;
using Xunit;
using Xunit.Abstractions;

namespace SkillFunctionalTests.SkillBot
{
    public class SkillBotTests
    {
        private readonly ILogger<SimpleHostBotToEchoSkillTest> _logger;
        private readonly string _transcriptsFolder = Directory.GetCurrentDirectory() + @"/SourceTranscripts/SkillBot";

        public SkillBotTests(ITestOutputHelper output)
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
        [InlineData("SkillBot.SelectEcho.transcript")]
        [InlineData("SkillBot.SelectBookFlight.transcript")]
        [InlineData("SkillBot.SelectGetWeather.transcript")]
        public async Task SelectSkillTests(string transcript)
        {
            // C:\Projects\Repos\BotFramework-FunctionalTests\SkillsFunctionalTests\tests\SkillFunctionalTests\SourceTranscripts
            var runner = new TestRunner(new TestClientFactory(ClientType.DirectLine).GetTestClient(), _logger);
            await runner.RunTestAsync(Path.Combine(_transcriptsFolder, transcript));
        }
    }
}
