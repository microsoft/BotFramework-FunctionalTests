// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder.Tests.Functional.Common;
using Xunit.Abstractions;

namespace Microsoft.Bot.Builder.Tests.Functional.Skills.Common
{
    public class SkillsTestBase : ScriptTestBase
    {
        public SkillsTestBase(ITestOutputHelper output)
            : base(output)
        {
        }

        public static List<string> DeliveryModes { get; } = new List<string>
        {
            Schema.DeliveryModes.Normal,
            Schema.DeliveryModes.ExpectReplies
        };

        public static List<HostBot> SimpleHostBots { get; } = new List<HostBot>
        {
            HostBot.SimpleHostBotComposerDotNet,
            HostBot.SimpleHostBotDotNet,
            HostBot.SimpleHostBotDotNet31,
            HostBot.SimpleHostBotJS,
            HostBot.SimpleHostBotPython,
        };

        public static List<HostBot> WaterfallHostBots { get; } = new List<HostBot>
        {
            HostBot.ComposerHostBotDotNet,
            HostBot.WaterfallHostBotDotNet,
            HostBot.WaterfallHostBotJS,
            HostBot.WaterfallHostBotPython,
        };

        public static List<HostBot> SimpleHostMSIBots { get; } = new List<HostBot>
        {
            HostBot.SimpleHostBotDotNetMSI,
            HostBot.SimpleHostBotJSMSI,
        };

        public static List<HostBot> SimpleHostSTBots { get; } = new List<HostBot>
        {
            HostBot.SimpleHostBotDotNetST,
            HostBot.SimpleHostBotJSST,
        };

        public static List<SkillBot> EchoSkillBots { get; } = new List<SkillBot>
        {
            SkillBot.EchoSkillBotComposerDotNet,
            SkillBot.EchoSkillBotDotNet,
            SkillBot.EchoSkillBotDotNet31,
            SkillBot.EchoSkillBotDotNetV3,
            SkillBot.EchoSkillBotJS,
            SkillBot.EchoSkillBotJSV3,
            SkillBot.EchoSkillBotPython
        };

        public static List<SkillBot> WaterfallSkillBots { get; } = new List<SkillBot>
        {
            SkillBot.WaterfallSkillBotDotNet,
            SkillBot.WaterfallSkillBotJS,
            SkillBot.WaterfallSkillBotPython,
            SkillBot.ComposerSkillBotDotNet
        };

        public static List<SkillBot> EchoSkillMSIBots { get; } = new List<SkillBot>
        {
            SkillBot.EchoSkillBotDotNetMSI,
            SkillBot.EchoSkillBotJSMSI
        };

        public static List<SkillBot> EchoSkillSTBots { get; } = new List<SkillBot>
        {
            SkillBot.EchoSkillBotDotNetST,
            SkillBot.EchoSkillBotJSST
        };

        public static IEnumerable<object[]> BuildTestCases(
            List<string> scripts,
            List<HostBot> hosts,
            List<SkillBot> skills,
            List<string> channels = default,
            List<string> deliveryModes = default,
            Func<SkillsTestCase, bool> exclude = default)
        {
            var cases = from channel in channels ?? Channels
                        from deliveryMode in deliveryModes ?? DeliveryModes
                        from bot in hosts
                        from skill in skills
                        from script in scripts
                        select new SkillsTestCase
                        {
                            Channel = channel,
                            DeliveryMode = deliveryMode,
                            Bot = bot,
                            Skill = skill,
                            Script = script
                        };

            return cases
                .Where(e => exclude == null || !exclude(e))
                .Select(e => new object[] { new TestCaseDataObject<SkillsTestCase>(e) });
        }
    }
}
