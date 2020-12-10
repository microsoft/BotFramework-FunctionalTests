// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallHostBot.Skills
{
    public class WaterfallSkill : SkillDefinition
    {
        private const string SkillActionCards = "Cards";
        private const string SkillActionProactive = "Proactive";
        private const string SkillActionAuth = "Auth";
        private const string SkillActionAttachment = "Attachment";

        public override IList<string> GetActions()
        {
            return new List<string>
            {
                SkillActionCards,
                SkillActionProactive,
                SkillActionAuth,
                SkillActionAttachment
            };
        }

        public override Activity CreateBeginActivity(string actionId)
        {
            Activity activity;

            // Send an event activity to the skill with "Cards" in the name.
            if (actionId.Equals(SkillActionCards, StringComparison.CurrentCultureIgnoreCase))
            {
                activity = (Activity)Activity.CreateEventActivity();
                activity.Name = SkillActionCards;
                return activity;
            }

            // Send an event activity to the skill with "Proactive" in the name.
            if (actionId.Equals(SkillActionProactive, StringComparison.CurrentCultureIgnoreCase))
            {
                activity = (Activity)Activity.CreateEventActivity();
                activity.Name = SkillActionProactive;
                return activity;
            }

            // Send an event activity to the skill with "Auth" in the name.
            if (actionId.Equals(SkillActionAuth, StringComparison.CurrentCultureIgnoreCase))
            {
                activity = (Activity)Activity.CreateEventActivity();
                activity.Name = SkillActionAuth;
                return activity;
            }

            // Send an event activity to the skill with "Attachment" in the name.
            if (actionId.Equals(SkillActionAttachment, StringComparison.CurrentCultureIgnoreCase))
            {
                activity = (Activity)Activity.CreateEventActivity();
                activity.Name = SkillActionAttachment;
                return activity;
            }

            throw new InvalidOperationException($"Unable to create begin activity for \"{actionId}\".");
        }
    }
}
