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

namespace SkillFunctionalTests.CardActions
{
    [Trait("TestCategory", "CardActions")]
    public class CardActionsTests : ScriptTestBase
    {
        private readonly string _testScriptsFolder = Directory.GetCurrentDirectory() + @"/CardActions/SourceTestScripts";
        private readonly string _transcriptsFolder = Directory.GetCurrentDirectory() + @"/CardActions/SourceTranscripts";

        public CardActionsTests(ITestOutputHelper output)
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
                HostBotNames.WaterfallHostBotDotNet,

                // TODO: Enable these when the ports to JS, Python and composer are ready
                //HostBotNames.WaterfallHostBotJS,
                //HostBotNames.WaterfallHostBotPython,
                //HostBotNames.ComposerHostBotDotNet
            };

            var targetSkills = new List<string>
            {
                SkillBotNames.WaterfallSkillBotDotNet,

                // TODO: Enable these when the ports to JS, Python and composer are ready
                //SkillBotNames.WaterfallSkillBotJS,
                //SkillBotNames.WaterfallSkillBotPython,
                //SkillBotNames.ComposerSkillBotDotNet
            };

            var scripts = new List<string>
            {
                "BotAction.json",
                "TaskModule.json",
                "SubmitAction.json",
                "Hero.json",
                "Thumbnail.json",
                "Receipt.json",
                "SignIn.json",
                "Carousel.json",
                "List.json",
                "O365.json",
                "File.json",
                "Animation.json",
                "Audio.json",
                "Video.json",
                "UploadFile.json"
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

            var options = TestClientOptions.FirstOrDefault(option => option.BotId == testCase.HostBot);
            var runner = new XUnitTestRunner(new TestClientFactory(testCase.ClientType, options).GetTestClient(), Logger);

            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, "WaterfallGreeting.json"));
            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, testCase.Script));
        }
    }
}
