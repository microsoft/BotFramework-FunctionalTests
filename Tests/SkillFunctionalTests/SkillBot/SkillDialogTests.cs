// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using SkillFunctionalTests.XUnit;
using TranscriptTestRunner;
using TranscriptTestRunner.XUnit;
using Xunit;
using Xunit.Abstractions;

namespace SkillFunctionalTests.SkillBot
{
    public class SkillDialogTests
    {
        private readonly string _transcriptsFolder = Directory.GetCurrentDirectory() + @"//SourceTranscripts/SkillDialog";
        private readonly ILogger<SimpleHostBotToEchoSkillTest> _logger;

        public SkillDialogTests(ITestOutputHelper output)
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
        [InlineData("SkillBot.BookFlight.transcript")]
        [InlineData("SkillBot.BookFlightWithParameters.transcript")]
        [InlineData("SkillBot.GetWeather.transcript")]
        [InlineData("SkillBot.EchoThroughDialog.transcript")]
        [InlineData("Demo.transcript")]
        public async Task SkillDialogActionsTests(string transcript)
        {
            _logger.LogInformation($"*******{transcript}********");

            var runner = new XUnitTestRunner(new TestClientFactory(ClientType.DirectLine).GetTestClient(), _logger);
            await runner.RunTestAsync(Path.Combine(_transcriptsFolder, transcript));
        }

        [Fact]
        public async Task ManualTest()
        {
            _logger.LogInformation($"*******ManualTest********");
            var runner = new TestRunner(new TestClientFactory(ClientType.DirectLine).GetTestClient(), _logger);
            
            await runner.SendActivityAsync(new Activity(ActivityTypes.ConversationUpdate));
            await runner.AssertReplyAsync(activity =>
            {
                Assert.Equal(ActivityTypes.Message, activity.Type);
                Assert.Equal("Welcome to the Dialog Skill Prototype!", activity.Speak);
            });
            await runner.AssertReplyAsync(activity =>
            {
                Assert.Equal(ActivityTypes.Message, activity.Type);
                Assert.Equal("What skill would you like to call?", activity.Speak);
            });

            await runner.SendActivityAsync(new Activity(ActivityTypes.Message, text: "EchoSkillBot"));
            await runner.AssertReplyAsync(activity =>
            {
                Assert.StartsWith("Select an action # to send to **EchoSkillBot**.", activity.Text);
            });
        }
    }
}
