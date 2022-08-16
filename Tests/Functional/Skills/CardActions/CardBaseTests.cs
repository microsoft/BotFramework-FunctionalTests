// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Testing.TestRunner;
using Microsoft.Bot.Builder.Testing.TestRunner.XUnit;
using Microsoft.Bot.Builder.Tests.Functional.Common;
using Microsoft.Bot.Builder.Tests.Functional.Skills.Common;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace Microsoft.Bot.Builder.Tests.Functional.Skills.CardActions
{
    public abstract class CardBaseTests : SkillsTestBase
    {
        private readonly string _testScriptsFolder = Directory.GetCurrentDirectory() + @"/Skills/CardActions/TestScripts";

        public CardBaseTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public static IEnumerable<object[]> TestCases(List<string> scripts = default, Func<SkillsTestCase, bool> exclude = default)
        {
            return BuildTestCases(scripts: scripts, hosts: WaterfallHostBots, skills: WaterfallSkillBots, exclude: exclude);
        }

        public virtual async Task RunTestCases(TestCaseDataObject<SkillsTestCase> testData)
        {
            var testCase = testData.GetObject();
            Logger.LogInformation(JsonConvert.SerializeObject(testCase, Formatting.Indented));

            var options = TestClientOptions[testCase.Bot];
            var runner = new XUnitTestRunner(new TestClientFactory(testCase.Channel, options, Logger).GetTestClient(), TestRequestTimeout, ThinkTime, Logger);

            var testParams = new Dictionary<string, string>
            {
                { "DeliveryMode", testCase.DeliveryMode },
                { "TargetSkill", testCase.Skill.ToString() }
            };

            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, "WaterfallGreeting.json"), testParams);
            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, testCase.Script), testParams);
        }
    }
}
