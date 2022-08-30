// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Builder.Tests.Functional.Skills.Common
{
    public enum HostBot
    {
        /// <summary>
        /// Waterfall MultiTenant host implemented using Composer and the DotNet runtime.
        /// </summary>
        ComposerHostBotDotNet,

        /// <summary>
        /// Simple MultiTenant host implemented using Composer and the DotNet runtime.
        /// </summary>
        SimpleHostBotComposerDotNet,

        /// <summary>
        /// Simple MultiTenant host implemented using DotNet 3.1.
        /// </summary>
        SimpleHostBotDotNet,

        /// <summary>
        /// Simple host implemented using dotnet 3.1.
        /// </summary>
        SimpleHostBotDotNet31,

        /// <summary>
        /// Simple UserAssignedMSI host implemented using DotNet 6.0.
        /// </summary>
        SimpleHostBotDotNetMSI,

        /// <summary>
        /// Simple SingleTenant host implemented using DotNet 6.0.
        /// </summary>
        SimpleHostBotDotNetST,

        /// <summary>
        /// Simple MultiTenant host implemented using JS.
        /// </summary>
        SimpleHostBotJS,

        /// <summary>
        /// Simple UserAssignedMSI host implemented using JS.
        /// </summary>
        SimpleHostBotJSMSI,

        /// <summary>
        /// Simple SingleTenant host implemented using JS.
        /// </summary>
        SimpleHostBotJSST,

        /// <summary>
        /// Simple MultiTenant host implemented using Python.
        /// </summary>
        SimpleHostBotPython,

        /// <summary>
        /// Waterfall MultiTenant host implemented using DotNet and waterfall dialogs.
        /// </summary>
        WaterfallHostBotDotNet,

        /// <summary>
        /// Waterfall MultiTenant host implemented using JS and waterfall dialogs.
        /// </summary>
        WaterfallHostBotJS,

        /// <summary>
        /// Waterfall MultiTenant host implemented using Python and waterfall dialogs.
        /// </summary>
        WaterfallHostBotPython,
    }
}
