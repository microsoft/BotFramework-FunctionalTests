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
    [Trait("TestCategory", "SingleTurn")]
    public class EchoComposerTests : ScriptTestBase
    {
        private readonly string _testScriptsFolder = Directory.GetCurrentDirectory() + @"/SingleTurn/TestScripts";

        public EchoComposerTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public static IEnumerable<object[]> TestCases()
        {
            var clientTypes = new List<ClientType> { ClientType.DirectLine };
            var deliverModes = new List<string>
            {
                DeliveryModes.Normal
            };
            
            var hostBots = new List<HostBot>
            {
                 HostBot.SimpleHostBotComposerDotNet
            };

            var targetSkills = new List<string>
            {
                SkillBotNames.ComposerSkillBotDotNet
            };

            var scripts = new List<string>
            {
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

            var options = TestClientOptions[testCase.HostBot];
            var runner = new XUnitTestRunner(new TestClientFactory(testCase.ClientType, options, Logger).GetTestClient(), TestRequestTimeout, Logger);

            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, testCase.Script));
        }
    }
}
