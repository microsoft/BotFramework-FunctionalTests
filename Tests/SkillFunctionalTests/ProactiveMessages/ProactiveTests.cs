// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SkillFunctionalTests.Common;
using TranscriptTestRunner;
using TranscriptTestRunner.XUnit;
using Xunit;
using Xunit.Abstractions;

namespace SkillFunctionalTests.ProactiveMessages
{
    [Trait("TestCategory", "ProactiveMessages")]
    public class ProactiveTests : ScriptTestBase
    {
        private readonly string _testScriptsFolder = Directory.GetCurrentDirectory() + @"/ProactiveMessages/SourceTestScripts";
        private readonly string _transcriptsFolder = Directory.GetCurrentDirectory() + @"/ProactiveMessages/SourceTranscripts";

        public ProactiveTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public static IEnumerable<object[]> TestCases()
        {
            var clientTypes = new List<ClientType> { ClientType.DirectLine };
            var deliverModes = new List<string>
            {
                DeliveryModes.Normal,
            };

            var hostBots = new List<HostBot>
            {
                HostBot.WaterfallHostBotDotNet,

                // TODO: Enable these when the ports to JS, Python and composer are ready
                //HostBotNames.WaterfallHostBotJS,
                //HostBotNames.WaterfallHostBotPython,
                //HostBotNames.ComposerHostBotDotNet
            };

            var targetSkills = new List<string>
            {
                SkillBotNames.WaterfallSkillBotDotNet,

                // TODO: Enable these when the ports to JS, Python and composer are ready
                //SkillBotNames.WaterfallSkillBotJS,
                //SkillBotNames.WaterfallSkillBotPython,
                //SkillBotNames.ComposerSkillBotDotNet
            };

            var scripts = new List<string>
            {
                "ProactiveStart.json",
            };

            var testCaseBuilder = new TestCaseBuilder();

            var testCases = testCaseBuilder.BuildTestCases(clientTypes, deliverModes, hostBots, targetSkills, scripts);
            foreach (var testCase in testCases)
            {
                yield return testCase;
            }
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public async Task RunTestCases(TestCaseDataObject testData)
        {
            var messageId = string.Empty;
            var url = string.Empty;

            var testCase = testData.GetObject<TestCase>();
            Logger.LogInformation(JsonConvert.SerializeObject(testCase, Formatting.Indented));

            var options = TestClientOptions[testCase.HostBot];
            var runner = new XUnitTestRunner(new TestClientFactory(testCase.ClientType, options, Logger).GetTestClient(), TestRequestTimeout, Logger);
            
            // Execute the first part of the conversation.
            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, testCase.Script));

            await runner.AssertReplyAsync(activity =>
            {
                Assert.Equal(ActivityTypes.Message, activity.Type);
                Assert.Contains("Navigate to https:", activity.Text);

                var message = activity.Text.Split(" ");
                url = message[2];
                messageId = url.Split("message=")[1];
            });

            // Get to the message's url
            using (var client = new HttpClient())
            {
                await client.GetAsync(url).ConfigureAwait(false);
            }

            var testParams = new Dictionary<string, string>
            {
                { "MessageId", messageId }
            };

            // Execute the rest of the conversation passing the messageId.
            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, "ProactiveEnd.json"), testParams);
        }
    }
}
