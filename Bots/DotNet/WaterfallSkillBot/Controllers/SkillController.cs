﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.FunctionalTestsBots.WaterfallSkillBot.Controllers
{
    /// <summary>
    /// A controller that handles skill replies to the bot.
    /// This example uses the <see cref="CloudSkillHandler"/> that is registered as a <see cref="ChannelServiceHandlerBase"/> in startup.cs.
    /// </summary>
    [ApiController]
    [Route("api/skills")]
    public class SkillController : ChannelServiceController
    {
        public SkillController(ChannelServiceHandlerBase handler)
            : base(handler)
        {
        }

        public override Task<IActionResult> ReplyToActivityAsync(string conversationId, string activityId, Activity activity)
        {
            try
            {
                return base.ReplyToActivityAsync(conversationId, activityId, activity);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        public override Task<IActionResult> SendToConversationAsync(string conversationId, Activity activity)
        {
            try
            {
                return base.SendToConversationAsync(conversationId, activity);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }
    }
}
