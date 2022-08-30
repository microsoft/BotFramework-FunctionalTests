// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Testing.TestRunner;
using Microsoft.Bot.Builder.Testing.TestRunner.XUnit;
using Microsoft.Bot.Builder.Tests.Functional.Common;
using Microsoft.Bot.Builder.Tests.Functional.Skills.Common;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Bot.Builder.Tests.Functional.Skills.MessageWithAttachment
{
    public class MessageWithAttachmentTests : SkillsTestBase
    {
        private static readonly List<string> Scripts = new List<string>
        {
            "MessageWithAttachment.json"
        };

        private readonly string _testScriptsFolder = Directory.GetCurrentDirectory() + @"/Skills/MessageWithAttachment/TestScripts";

        public MessageWithAttachmentTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public static IEnumerable<object[]> TestCases() => BuildTestCases(scripts: Scripts, hosts: WaterfallHostBots, skills: WaterfallSkillBots);

        [Theory]
        [MemberData(nameof(TestCases))]
        public async Task RunTestCases(TestCaseDataObject<SkillsTestCase> testData)
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

            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, testCase.Script), testParams);
        }
    }
}
