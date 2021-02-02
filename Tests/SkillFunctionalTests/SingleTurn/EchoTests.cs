﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
    public class EchoTests : ScriptTestBase
    {
        private readonly string _testScriptsFolder = Directory.GetCurrentDirectory() + @"/SingleTurn/TestScripts";

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
                DeliveryModes.ExpectReplies
            };

            var hostBots = new List<HostBot>
            {
                HostBot.SimpleHostBotComposerDotNet,
                HostBot.SimpleHostBotDotNet,
                HostBot.SimpleHostBotDotNet21,
                HostBot.SimpleHostBotJS,
                HostBot.SimpleHostBotPython,
            };

            var targetSkills = new List<string>
            {
                SkillBotNames.EchoSkillBotDotNet,
                SkillBotNames.EchoSkillBotDotNet21,
                SkillBotNames.EchoSkillBotDotNetV3,
                SkillBotNames.EchoSkillBotJS,
                SkillBotNames.EchoSkillBotJSV3,
                SkillBotNames.EchoSkillBotPython
            };

            var scripts = new List<string> { "EchoMultiSkill.json" };

            var testCaseBuilder = new TestCaseBuilder();

            // This local function is used to exclude ExpectReplies test cases for v3 bots
            static bool ShouldExclude(TestCase testCase)
            {
                if (testCase.DeliveryMode == DeliveryModes.ExpectReplies)
                {
                    if (testCase.TargetSkill == SkillBotNames.EchoSkillBotDotNetV3 || testCase.TargetSkill == SkillBotNames.EchoSkillBotJSV3 || testCase.HostBot == HostBot.SimpleHostBotComposerDotNet)
                    {
                        return true;
                    }
                }

                return false;
            }

            var testCases = testCaseBuilder.BuildTestCases(clientTypes, deliverModes, hostBots, targetSkills, scripts, ShouldExclude);
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

            var testParams = new Dictionary<string, string>
            {
                { "DeliveryMode", testCase.DeliveryMode },
                { "TargetSkill", testCase.TargetSkill }
            };

            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, testCase.Script), testParams);
        }
    }
}
