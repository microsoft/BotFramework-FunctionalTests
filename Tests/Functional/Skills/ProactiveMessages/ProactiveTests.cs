// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Testing.TestRunner;
using Microsoft.Bot.Builder.Testing.TestRunner.XUnit;
using Microsoft.Bot.Builder.Tests.Functional.Common;
using Microsoft.Bot.Builder.Tests.Functional.Skills.Common;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Bot.Builder.Tests.Functional.Skills.ProactiveMessages
{
    public class ProactiveTests : SkillsTestBase
    {
        private static readonly List<string> Scripts = new List<string>
        {
            "ProactiveStart.json"
        };

        private readonly string _testScriptsFolder = Directory.GetCurrentDirectory() + @"/Skills/ProactiveMessages/TestScripts";

        public ProactiveTests(ITestOutputHelper output)
            : base(output)
        {
        }

        // TODO: Enable after Composer supports proactive messages.
        public static bool Exclude(SkillsTestCase test) => test.Skill == SkillBot.ComposerSkillBotDotNet;

        public static IEnumerable<object[]> TestCases() => BuildTestCases(scripts: Scripts, hosts: WaterfallHostBots, skills: WaterfallSkillBots, exclude: Exclude);

        [Theory]
        [MemberData(nameof(TestCases))]
        public async Task RunTestCases(TestCaseDataObject<SkillsTestCase> testData)
        {
            var userId = string.Empty;
            var url = string.Empty;

            var testCase = testData.GetObject();
            Logger.LogInformation(JsonConvert.SerializeObject(testCase, Formatting.Indented));

            var options = TestClientOptions[testCase.Bot];
            var runner = new XUnitTestRunner(new TestClientFactory(testCase.Channel, options, Logger).GetTestClient(), TestRequestTimeout, ThinkTime, Logger);
            
            var testParamsStart = new Dictionary<string, string>
            {
                { "DeliveryMode", testCase.DeliveryMode },
                { "TargetSkill", testCase.Skill.ToString() }
            };

            // Execute the first part of the conversation.
            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, testCase.Script), testParamsStart);

            await runner.AssertReplyAsync(activity =>
            {
                Assert.Equal(ActivityTypes.Message, activity.Type);
                Assert.Contains("Navigate to http", activity.Text);

                var message = activity.Text.Split(" ");
                url = message[2];
                userId = url.Split("user=")[1];
            });

            // Send a get request to the message's url to continue the conversation.
            using (var client = new HttpClient())
            {
                await client.GetAsync(url).ConfigureAwait(false);
            }

            var testParamsEnd = new Dictionary<string, string>
            {
                { "UserId", userId },
                { "TargetSkill", testCase.Skill.ToString() }
            };

            // Execute the rest of the conversation passing the messageId.
            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, "ProactiveEnd.json"), testParamsEnd);
        }
    }
}
