// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TranscriptTestRunner;
using TranscriptTestRunner.XUnit;
using Xunit;
using Xunit.Abstractions;
using Activity = Microsoft.Bot.Schema.Activity;
using ActivityTypes = Microsoft.Bot.Connector.DirectLine.ActivityTypes;

namespace SkillFunctionalTests
{
    [Trait("TestCategory", "FunctionalTests")]
    [Trait("TestCategory", "OAuth")]
    [Trait("TestCategory", "SkipForV3Bots")]
    public class OAuthSkillTest
    {
        private readonly string _transcriptsFolder = Directory.GetCurrentDirectory() + @"/SourceTranscripts";
        private readonly ILogger<OAuthSkillTest> _logger;

        public OAuthSkillTest(ITestOutputHelper output)
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

            _logger = loggerFactory.CreateLogger<OAuthSkillTest>();
        }

        [Theory]
        [InlineData("ShouldSignIn.transcript")]
        public async Task RunScripts(string transcript)
        {
            var runner = new XUnitTestRunner(new TestClientFactory(ClientType.DirectLine).GetTestClient(), _logger);
            await runner.RunTestAsync(Path.Combine(_transcriptsFolder, transcript));
        }

        [Fact]
        public async Task ManualTest()
        {
            var runner = new XUnitTestRunner(new TestClientFactory(ClientType.DirectLine).GetTestClient(), _logger);

            await runner.SendActivityAsync(new Activity(ActivityTypes.ConversationUpdate));
            await runner.AssertReplyAsync(activity =>
            {
                Assert.Equal(ActivityTypes.Message, activity.Type);
                Assert.Equal("Hello and welcome!", activity.Text);
            });

            await runner.SendActivityAsync(new Activity() { Type = ActivityTypes.Message, Text = "skill" });
            await runner.AssertReplyAsync(activity =>
            {
                Assert.Equal(ActivityTypes.Message, activity.Type);
                Assert.Equal("Got it, connecting you to the skill...", activity.Text);
            });
            await runner.AssertReplyAsync(activity =>
            {
                Assert.Equal(ActivityTypes.Message, activity.Type);
                Assert.Equal("Echo: skill", activity.Text);
            });
            await runner.AssertReplyAsync(activity =>
            {
                Assert.Equal(ActivityTypes.Message, activity.Type);
                Assert.Equal("Say \"end\" or \"stop\" and I\'ll end the conversation and back to the parent.", activity.Text);
            });

            await runner.SendActivityAsync(new Activity() { Type = ActivityTypes.Message, Text = "auth" });
            await runner.AssertReplyAsync(async activity =>
            {
                Assert.Equal(ActivityTypes.Message, activity.Type);
                Assert.True(activity.Attachments.Count > 0);
                await runner.SignInAndVerifyOAuthAsync(activity);
            });
            await runner.AssertReplyAsync(activity =>
            {
                Assert.Equal(ActivityTypes.Message, activity.Type);
                Assert.Equal("You are now logged in.", activity.Text);
            });
        }
    }
}
