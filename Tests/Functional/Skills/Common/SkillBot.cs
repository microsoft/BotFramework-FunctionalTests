// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Builder.Tests.Functional.Skills.Common
{
    public enum SkillBot
    {
        /// <summary>
        /// Echo MultiTenant skill implemented using Composer and the DotNet runtime.
        /// </summary>
        EchoSkillBotComposerDotNet,

        /// <summary>
        /// Echo MultiTenant skill implemented using DotNet 6.0.
        /// </summary>
        EchoSkillBotDotNet,

        /// <summary>
        /// Echo MultiTenant skill implemented using dotnet 3.1.
        /// </summary>
        EchoSkillBotDotNet31,

        /// <summary>
        /// Echo UserAssignedMSI skill implemented using DotNet 6.0.
        /// </summary>
        EchoSkillBotDotNetMSI,

        /// <summary>
        /// Echo SingleTenant skill implemented using DotNet 6.0.
        /// </summary>
        EchoSkillBotDotNetST,

        /// <summary>
        /// Echo MultiTenant v3 skill implemented using DotNet 6.0.
        /// </summary>
        EchoSkillBotDotNetV3,

        /// <summary>
        /// Echo MultiTenant skill implemented using JS.
        /// </summary>
        EchoSkillBotJS,

        /// <summary>
        /// Echo UserAssignedMSI skill implemented using JS.
        /// </summary>
        EchoSkillBotJSMSI,

        /// <summary>
        /// Echo SingleTenant skill implemented using JS.
        /// </summary>
        EchoSkillBotJSST,

        /// <summary>
        /// Echo MultiTenant v3 skill implemented using JS.
        /// </summary>
        EchoSkillBotJSV3,

        /// <summary>
        /// Echo MultiTenant skill implemented using Python.
        /// </summary>
        EchoSkillBotPython,

        /// <summary>
        /// Waterfall MultiTenant skill implemented using DotNet 6.0.
        /// </summary>
        WaterfallSkillBotDotNet,

        /// <summary>
        /// Waterfall MultiTenant skill implemented using JS.
        /// </summary>
        WaterfallSkillBotJS,

        /// <summary>
        /// Waterfall MultiTenant skill implemented using Python.
        /// </summary>
        WaterfallSkillBotPython,

        /// <summary>
        /// Waterfall MultiTenant skill implemented using Composer and the DotNet runtime.
        /// </summary>
        ComposerSkillBotDotNet,
    }
}
