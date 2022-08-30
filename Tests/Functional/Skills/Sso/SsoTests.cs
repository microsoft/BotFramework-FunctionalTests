// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Tests.Functional.Common;
using Microsoft.Bot.Builder.Tests.Functional.Skills.Common;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Bot.Builder.Tests.Functional.Skills.Sso
{
    public class SsoTests : SkillsTestBase
    {
        private static readonly List<string> Scripts = new List<string>
        {
            "Sso.json"
        };

        private readonly string _testScriptsFolder = Directory.GetCurrentDirectory() + @"/Skills/Sso/TestScripts";

        public SsoTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public static IEnumerable<object[]> TestCases() => BuildTestCases(scripts: Scripts, hosts: WaterfallHostBots, skills: WaterfallSkillBots);

        [Theory]
        [MemberData(nameof(TestCases))]
        public Task RunTestCases(TestCaseDataObject<SkillsTestCase> testData)
        {
            var testCase = testData.GetObject();
            Logger.LogInformation(JsonConvert.SerializeObject(testCase, Formatting.Indented));
            
            // TODO: Implement tests and scripts
            //var runner = new XUnitTestRunner(new TestClientFactory(testCase.ChannelId).GetTestClient(), Logger);
            //await runner.RunTestAsync(Path.Combine(_testScriptsFolder, testCase.Script));

            // TODO: remove this line once we implement the test and we change the method to public async task
            return Task.CompletedTask;
        }
    }
}
