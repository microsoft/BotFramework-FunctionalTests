// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TranscriptTestRunner;
using Xunit;
using Xunit.Abstractions;

namespace SkillFunctionalTests
{
    public class SimpleHostBotToDialogSkillTest
    {
        private readonly string _transcriptsFolder = Directory.GetCurrentDirectory() + @"/SourceTranscripts";
        private readonly string _testScriptsFolder = Directory.GetCurrentDirectory() + @"/SourceTestScripts";
        private readonly ILogger<SimpleHostBotToEchoSkillTest> _logger;

        public SimpleHostBotToDialogSkillTest(ITestOutputHelper output)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConfiguration(configuration)
                    .AddConsole()
                    .AddDebug()
                    .AddFile(Directory.GetCurrentDirectory() + @"/Logs/Log.json", isJson: true)
                    .AddXunit(output);
            });

            _logger = loggerFactory.CreateLogger<SimpleHostBotToEchoSkillTest>();
        }

        [Fact]
        public async Task DialogSkillShouldConnect()
        {
            var runner = new TestRunner(new TestClientFactory(ClientType.DirectLine).GetTestClient(), _logger);
            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, "DialogSkill.json"));
        }
    }
}
