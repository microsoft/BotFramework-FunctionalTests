// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Threading.Tasks;
using TranscriptTestRunner;
using TranscriptTestRunner.XUnit;
using Xunit;
using Xunit.Abstractions;

namespace SkillFunctionalTests.LegacyTests
{
    public class SimpleHostBotToDialogSkillTest : ScriptTestBase
    {
        private readonly string _transcriptsFolder = Directory.GetCurrentDirectory() + @"/LegacyTests/SourceTranscripts";
        private readonly string _testScriptsFolder = Directory.GetCurrentDirectory() + @"/LegacyTests/SourceTestScripts";

        public SimpleHostBotToDialogSkillTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Skip = "Skipped until DialogSkillBot is supported.")]
        public async Task DialogSkillShouldConnect()
        {
            var runner = new XUnitTestRunner(new TestClientFactory(ClientType.DirectLine, TestClientOptions[0]).GetTestClient(), Logger);
            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, "DialogSkill.json"));
        }
    }
}
