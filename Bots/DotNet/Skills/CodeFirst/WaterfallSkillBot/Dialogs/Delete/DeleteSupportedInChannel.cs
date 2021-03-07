﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.Connector;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Delete
{
    public class DeleteSupportedInChannel
    {
        private static readonly HashSet<string> DeleteSupported = new HashSet<string>
        {
            Channels.Msteams,
            Channels.Slack,
            Channels.Telegram
        };

        /// <summary>
        /// This let's you know if a card is supported in a given channel.
        /// </summary>
        /// <param name="channel">Bot Connector Channel.</param>
        /// <returns>A bool if the card is supported in the channel.</returns>
        public static bool IsDeleteSupported(string channel)
        {
            return DeleteSupported.Contains(channel.ToString());
        }
    }
}
