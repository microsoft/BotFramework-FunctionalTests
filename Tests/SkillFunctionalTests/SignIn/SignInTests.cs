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
using Xunit;
using Xunit.Abstractions;

namespace SkillFunctionalTests.SignIn
{
    public class SignInTests : ScriptTestBase
    {
        private readonly string _testScriptsFolder = Directory.GetCurrentDirectory() + @"/SignIn/TestScripts";

        public SignInTests(ITestOutputHelper output)
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

            var hostBots = new List<HostBot>
            {
                HostBot.WaterfallHostBotDotNet,
                HostBot.WaterfallHostBotJS,
                HostBot.WaterfallHostBotPython
            };

            var targetSkills = new List<string>
            {
                SkillBotNames.WaterfallSkillBotDotNet,
                SkillBotNames.WaterfallSkillBotJS,
                SkillBotNames.WaterfallSkillBotPython
            };

            var scripts = new List<string>
            {
                "SignIn.json",
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
        public Task RunTestCases(TestCaseDataObject testData)
        {
            var testCase = testData.GetObject<TestCase>();
            Logger.LogInformation(JsonConvert.SerializeObject(testCase, Formatting.Indented));
            
            // TODO: Implement tests and scripts
            //var runner = new XUnitTestRunner(new TestClientFactory(testCase.ClientType).GetTestClient(), Logger);
            //await runner.RunTestAsync(Path.Combine(_testScriptsFolder, testCase.Script));

            // TODO: remove this line once we implement the test and we change the method to public async task
            return Task.CompletedTask;
        }
    }
}
