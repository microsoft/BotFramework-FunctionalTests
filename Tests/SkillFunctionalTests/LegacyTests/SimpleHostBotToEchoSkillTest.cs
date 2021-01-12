// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using TranscriptTestRunner;
using TranscriptTestRunner.XUnit;
using Xunit;
using Xunit.Abstractions;
using ActivityTypes = Microsoft.Bot.Connector.DirectLine.ActivityTypes;

namespace SkillFunctionalTests.LegacyTests
{
    [Trait("TestCategory", "FunctionalTests")]
    public class SimpleHostBotToEchoSkillTest : ScriptTestBase
    {
        private readonly string _transcriptsFolder = Directory.GetCurrentDirectory() + @"/LegacyTests/SourceTranscripts";
        private readonly string _testScriptsFolder = Directory.GetCurrentDirectory() + @"/LegacyTests/SourceTestScripts";

        public SimpleHostBotToEchoSkillTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [Theory]
        [InlineData("ShouldRedirectToSkill.transcript")]
        [InlineData("HostReceivesEndOfConversation.transcript")]
        public async Task RunScripts(string transcript)
        {
            var runner = new XUnitTestRunner(new TestClientFactory(ClientType.DirectLine, TestClientOptions[0]).GetTestClient(), Logger);
            await runner.RunTestAsync(Path.Combine(_transcriptsFolder, transcript));
        }

        [Fact]
        public async Task ManualTest()
        {
            var runner = new XUnitTestRunner(new TestClientFactory(ClientType.DirectLine, TestClientOptions[0]).GetTestClient(), Logger);

            await runner.SendActivityAsync(new Activity(ActivityTypes.ConversationUpdate));

            await runner.AssertReplyAsync(activity =>
            {
                Assert.Equal(ActivityTypes.Message, activity.Type);
                Assert.Equal("Hello and welcome!", activity.Text);
            });
        }
    }
}
