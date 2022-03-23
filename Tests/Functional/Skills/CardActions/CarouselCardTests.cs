// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SkillFunctionalTests.Skills.Common;
using TranscriptTestRunner;
using TranscriptTestRunner.XUnit;
using Xunit;
using Xunit.Abstractions;

namespace SkillFunctionalTests.Skills.CardActions
{
    [Trait("TestCategory", "CardActions")]
    public class CarouselCardTests : ScriptTestBase
    {
        private readonly string _testScriptsFolder = Directory.GetCurrentDirectory() + @"/Skills/CardActions/TestScripts";

        public CarouselCardTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public static IEnumerable<object[]> TestCases()
        {
            var channelIds = new List<string> { Channels.Directline };
            
            var deliverModes = new List<string>
            {
                DeliveryModes.Normal,
                DeliveryModes.ExpectReplies
            };

            var hostBots = new List<HostBot>
            {
                HostBot.ComposerHostBotDotNet,
                HostBot.WaterfallHostBotDotNet,
                HostBot.WaterfallHostBotJS,
                HostBot.WaterfallHostBotPython,
            };

            var targetSkills = new List<string>
            {
                SkillBotNames.WaterfallSkillBotDotNet,
                SkillBotNames.WaterfallSkillBotJS,
                SkillBotNames.WaterfallSkillBotPython,
                SkillBotNames.ComposerSkillBotDotNet
            };

            var scripts = new List<string>
            {
                "Carousel.json"
            };

            var testCaseBuilder = new TestCaseBuilder();
            var testCases = testCaseBuilder.BuildTestCases(channelIds, deliverModes, hostBots, targetSkills, scripts);

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
            var runner = new XUnitTestRunner(new TestClientFactory(testCase.ChannelId, options, Logger).GetTestClient(), TestRequestTimeout, ThinkTime, Logger);

            var testParams = new Dictionary<string, string>
            {
                { "DeliveryMode", testCase.DeliveryMode },
                { "TargetSkill", testCase.TargetSkill }
            };

            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, "WaterfallGreeting.json"), testParams);
            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, testCase.Script), testParams);
        }
    }
}
