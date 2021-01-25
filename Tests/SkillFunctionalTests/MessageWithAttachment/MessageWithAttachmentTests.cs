// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SkillFunctionalTests.Common;
using TranscriptTestRunner;
using TranscriptTestRunner.XUnit;
using Xunit;
using Xunit.Abstractions;

namespace SkillFunctionalTests.MessageWithAttachment
{
    [Trait("TestCategory", "Attachments")]
    public class MessageWithAttachmentTests : ScriptTestBase
    {
        private readonly string _testScriptsFolder = Directory.GetCurrentDirectory() + @"/MessageWithAttachment/TestScripts";

        public MessageWithAttachmentTests(ITestOutputHelper output)
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
                "MessageWithAttachment.json",
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
            var testCase = testData.GetObject<TestCase>();
            Logger.LogInformation(JsonConvert.SerializeObject(testCase, Formatting.Indented));

            var options = TestClientOptions[testCase.HostBot];
            var runner = new XUnitTestRunner(new TestClientFactory(testCase.ClientType, options).GetTestClient(), Logger);

            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, testCase.Script));
        }
    }
}
