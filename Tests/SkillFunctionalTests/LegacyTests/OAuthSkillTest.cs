// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SkillFunctionalTests.Common;
using TranscriptTestRunner;
using TranscriptTestRunner.XUnit;
using Xunit;
using Xunit.Abstractions;
using ActivityTypes = Microsoft.Bot.Connector.DirectLine.ActivityTypes;
using SigninCard = Microsoft.Bot.Connector.DirectLine.SigninCard;

namespace SkillFunctionalTests.LegacyTests
{
    [Trait("TestCategory", "FunctionalTests")]
    [Trait("TestCategory", "Legacy")]
    [Trait("TestCategory", "OAuth")]
    [Trait("TestCategory", "SkipForV3Bots")]
    public class OAuthSkillTest : ScriptTestBase
    {
        private readonly string _testScriptsFolder = Directory.GetCurrentDirectory() + @"/LegacyTests/TestScripts";

        public OAuthSkillTest(ITestOutputHelper output)
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
                HostBot.EchoHostBot
            };

            var targetSkills = new List<string>
            {
                SkillBotNames.EchoSkillBot
            };

            var scripts = new List<string> { "ShouldSignIn1.transcript" };

            var testCaseBuilder = new TestCaseBuilder();

            // This local function is used to exclude ExpectReplies test cases for v3 bots
            static bool ShouldExclude(TestCase testCase)
            {
                if (testCase.DeliveryMode == DeliveryModes.ExpectReplies)
                {
                    if (testCase.TargetSkill == SkillBotNames.EchoSkillBotDotNetV3 || testCase.TargetSkill == SkillBotNames.EchoSkillBotJSV3 || testCase.TargetSkill == SkillBotNames.EchoSkillBotPython)
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

            var signInUrl = string.Empty;

            // Execute the first part of the conversation.
            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, testCase.Script), testParams);

            // Obtain the signIn url.
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

            // Execute the rest of the conversation.
            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, "ShouldSignIn2.transcript"));
        }
    }
}
