// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace Microsoft.Bot.Builder.Tests.Functional.Skills.SignIn
{
    public class SignInTests : SkillsTestBase
    {
        private static readonly List<string> Scripts = new List<string>
        {
            "SignIn1.json"
        };

        private readonly string _testScriptsFolder = Directory.GetCurrentDirectory() + @"/Skills/SignIn/TestScripts";

        public SignInTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public static IEnumerable<object[]> TestCases() => BuildTestCases(scripts: Scripts, hosts: WaterfallHostBots, skills: WaterfallSkillBots);

        [Theory]
        [MemberData(nameof(TestCases))]
        public async Task RunTestCases(TestCaseDataObject<SkillsTestCase> testData)
        {
            var signInUrl = string.Empty;
            var testCase = testData.GetObject();
            Logger.LogInformation(JsonConvert.SerializeObject(testCase, Formatting.Indented));

            var options = TestClientOptions[testCase.Bot];
            var runner = new XUnitTestRunner(new TestClientFactory(testCase.Channel, options, Logger).GetTestClient(), TestRequestTimeout, ThinkTime, Logger);

            var testParams = new Dictionary<string, string>
            {
                { "DeliveryMode", testCase.DeliveryMode },
                { "TargetSkill", testCase.Skill.ToString() }
            };

            // Execute the first part of the conversation.
            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, testCase.Script), testParams);

            await runner.AssertReplyAsync(activity =>
            {
                Assert.Equal(ActivityTypes.Message, activity.Type);
                Assert.True(activity.Attachments.Count > 0);

                var card = JsonConvert.DeserializeObject<SigninCard>(JsonConvert.SerializeObject(activity.Attachments.FirstOrDefault().Content));
                signInUrl = card.Buttons[0].Value?.ToString();

                Assert.False(string.IsNullOrEmpty(signInUrl));
            });

            // Execute the SignIn.
            await runner.ClientSignInAsync(signInUrl);

            // Execute the rest of the conversation passing the messageId.
            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, "SignIn2.json"), testParams);
        }
    }
}
