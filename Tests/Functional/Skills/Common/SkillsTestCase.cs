// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Tests.Functional.Common;

namespace Microsoft.Bot.Builder.Tests.Functional.Skills.Common
{
    public class SkillsTestCase : TestCase<HostBot>
    {
        public string DeliveryMode { get; set; }

        public SkillBot Skill { get; set; }

        public override string ToString()
        {
            return $"{Script}, {Bot}, {Skill}, {Channel}, {DeliveryMode}";
        }
    }
}
