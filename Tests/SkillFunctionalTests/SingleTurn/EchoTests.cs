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

namespace SkillFunctionalTests.SingleTurn
{
    public class EchoTests : ScriptTestBase
    {
        private readonly string _testScriptsFolder = Directory.GetCurrentDirectory() + @"/SingleTurn/SourceTestScripts";
        private readonly string _transcriptsFolder = Directory.GetCurrentDirectory() + @"/SingleTurn/SourceTranscripts";

        public EchoTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public static IEnumerable<object[]> TestCases()
        {
            var clientTypes = new List<ClientType> { ClientType.DirectLine };
            var deliverModes = new List<string>
            {
                DeliveryModes.Normal,
                DeliveryModes.ExpectReplies,
            };
            
            var hostBots = new List<string>
            {
                HostBotNames.SimpleHostBotDotNet,
                HostBotNames.SimpleHostBotDotNet21,
                HostBotNames.SimpleHostBotJS,
                HostBotNames.SimpleHostBotPython,
                HostBotNames.SimpleComposerHostBotDotNet
            };

            var targetSkills = new List<string>
            {
                SkillBotNames.EchoSkillBotDotNet,
                SkillBotNames.EchoSkillBot21DotNet,
                SkillBotNames.EchoSkillBotV3DotNet,
                SkillBotNames.EchoSkillBotJS,
                SkillBotNames.EchoSkillBotV3JS,
                SkillBotNames.EchoSkillBotPython
            };

            var scripts = new List<string>
            {
                "ShouldRedirectToSkill.json",
                "HostReceivesEndOfConversation.json"
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

            var runner = new XUnitTestRunner(new TestClientFactory(testCase.ClientType).GetTestClient(), Logger);
            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, testCase.Script));
        }
    }
}
