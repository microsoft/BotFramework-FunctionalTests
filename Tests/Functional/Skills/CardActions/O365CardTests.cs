// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Tests.Functional.Common;
using Microsoft.Bot.Builder.Tests.Functional.Skills.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Bot.Builder.Tests.Functional.Skills.CardActions
{
    public class O365CardTests : CardBaseTests
    {
        private static readonly List<string> Scripts = new List<string>
        {
            "O365.json"
        };

        public O365CardTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public static bool Exclude(SkillsTestCase test)
        {
            // BUG: O365 fails with ExpectReplies for WaterfallSkillBotPython (remove when https://github.com/microsoft/BotFramework-FunctionalTests/issues/328 is fixed).
            return test.Skill == SkillBot.WaterfallSkillBotPython && test.DeliveryMode == Schema.DeliveryModes.ExpectReplies;
        }

        public static IEnumerable<object[]> TestCases() => TestCases(scripts: Scripts, exclude: Exclude);

        [Theory]
        [MemberData(nameof(TestCases))]
        public override Task RunTestCases(TestCaseDataObject<SkillsTestCase> testData) => base.RunTestCases(testData);
    }
}
