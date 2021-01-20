// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Bot.Schema;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallHostBot.Skills
{
    public class WaterfallSkill : SkillDefinition
    {
        private const string SkillActionCards = "Cards";
        private const string SkillActionProactive = "Proactive";
        private const string SkillActionAuth = "Auth";
        private const string SkillActionMessageWithAttachment = "MessageWithAttachment";
        private const string SkillActionSso = "Sso";
        private const string SkillActionFileUpload = "FileUpload";
        private const string SkillActionCallEchoSkill = "Echo";

        public override IList<string> GetActions()
        {
            return new List<string>
            {
                SkillActionCards,
                SkillActionProactive,
                SkillActionAuth,
                SkillActionMessageWithAttachment,
                SkillActionSso,
                SkillActionFileUpload,
                SkillActionCallEchoSkill
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
            if (actionId.Equals(SkillActionMessageWithAttachment, StringComparison.CurrentCultureIgnoreCase))
            {
                activity = (Activity)Activity.CreateEventActivity();
                activity.Name = SkillActionMessageWithAttachment;
                return activity;
            }

            // Send an event activity to the skill with "Sso" in the name.
            if (actionId.Equals(SkillActionSso, StringComparison.CurrentCultureIgnoreCase))
            {
                activity = (Activity)Activity.CreateEventActivity();
                activity.Name = SkillActionSso;
                return activity;
            }

            // Send an event activity to the skill with "FileUpload" in the name.
            if (actionId.Equals(SkillActionFileUpload, StringComparison.CurrentCultureIgnoreCase))
            {
                activity = (Activity)Activity.CreateEventActivity();
                activity.Name = SkillActionFileUpload;
                return activity;
            }

            // Send an event activity to the skill with "Echo" in the name.
            if (actionId.Equals(SkillActionCallEchoSkill, StringComparison.CurrentCultureIgnoreCase))
            {
                activity = (Activity)Activity.CreateEventActivity();
                activity.Name = SkillActionCallEchoSkill;
                return activity;
            }

            throw new InvalidOperationException($"Unable to create begin activity for \"{actionId}\".");
        }
    }
}
