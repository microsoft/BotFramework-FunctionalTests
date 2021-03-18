// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SkillFunctionalTests.Common;
using TranscriptTestRunner;
using TranscriptTestRunner.XUnit;
using Xunit;
using Xunit.Abstractions;

namespace SkillFunctionalTests.FileUpload
{
    [Trait("TestCategory", "FileUpload")]
    public class FileUploadTests : ScriptTestBase
    {
        private readonly string _testScriptsFolder = Directory.GetCurrentDirectory() + @"/FileUpload/TestScripts";

        public FileUploadTests(ITestOutputHelper output)
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
                HostBot.WaterfallHostBotDotNet,

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
                "FileUpload1.json",
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
            const string fileName = "TestFile.txt";
            var testCase = testData.GetObject<TestCase>();
            Logger.LogInformation(JsonConvert.SerializeObject(testCase, Formatting.Indented));

            var options = TestClientOptions[testCase.HostBot];
            var runner = new XUnitTestRunner(new TestClientFactory(testCase.ChannelId, options, Logger).GetTestClient(), TestRequestTimeout, Logger);

            // Execute the first part of the conversation.
            var testParams = new Dictionary<string, string>
            {
                { "DeliveryMode", testCase.DeliveryMode },
                { "TargetSkill", testCase.TargetSkill },
                { "FileName", fileName },
                
                // Temp folder where the bots deployed in Azure save the uploaded file. Change the path to run the tests against local bots.
                { "BotsTempFolder", $"\\\"D:\\\\local\\\\Temp\\\\{fileName}\\\"" }
            };
            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, testCase.Script), testParams);

            await using var file = File.OpenRead(Directory.GetCurrentDirectory() + $"/FileUpload/media/{fileName}");

            await runner.UploadAsync(file);

            // Execute the rest of the conversation.
            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, "FileUpload2.json"), testParams);
        }
    }
}
