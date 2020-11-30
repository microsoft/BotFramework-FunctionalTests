// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TranscriptTestRunner;
using TranscriptTestRunner.XUnit;
using Xunit;
using Xunit.Abstractions;
using ActivityTypes = Microsoft.Bot.Connector.DirectLine.ActivityTypes;

namespace SkillFunctionalTests
{
    [Trait("TestCategory", "FunctionalTests")]
    public class SimpleHostBotToEchoSkillTest
    {
        private readonly string _transcriptsFolder = Directory.GetCurrentDirectory() + @"/SourceTranscripts";
        private readonly string _testScriptsFolder = Directory.GetCurrentDirectory() + @"/SourceTestScripts";
        private readonly ILogger<SimpleHostBotToEchoSkillTest> _logger;

        public SimpleHostBotToEchoSkillTest(ITestOutputHelper output)
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

        [Theory]
        [InlineData("ShouldRedirectToSkill.transcript")]
        [InlineData("HostReceivesEndOfConversation.transcript")]
        public async Task RunScripts(string transcript)
        {
            const int tries = 3;
            for (var index = 0; index < tries; index++)
            {
                try
                {
                    var runner = new XUnitTestRunner(new TestClientFactory(ClientType.DirectLine).GetTestClient(), _logger);
                    await runner.RunTestAsync(Path.Combine(_transcriptsFolder, transcript));
                }
                catch (TimeoutException)
                {
                    if (index + 1 == tries)
                    {
                        throw;
                    }
                    _logger.LogInformation($"======== Timeout exception on try number {index + 1}, starting retry... ========");
                }
            }
        }

        [Fact]
        public async Task ManualTest()
        {
            const int tries = 3;
            for (var index = 0; index < tries; index++)
            {
                try
                {
                    var runner = new XUnitTestRunner(new TestClientFactory(ClientType.DirectLine).GetTestClient(), _logger);

                    await runner.SendActivityAsync(new Activity(ActivityTypes.ConversationUpdate));

                    await runner.AssertReplyAsync(activity =>
                    {
                        Assert.Equal(ActivityTypes.Message, activity.Type);
                        Assert.Equal("Hello and welcome!", activity.Text);
                    });
                }
                catch (TimeoutException)
                {
                    if (index + 1 == tries)
                    {
                        throw;
                    }
                    _logger.LogInformation($"======== Timeout exception on try number {index + 1}, starting retry... ========");
                }
            }
        }
    }
}
