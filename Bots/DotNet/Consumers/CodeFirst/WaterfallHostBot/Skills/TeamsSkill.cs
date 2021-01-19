// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Bot.Schema;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallHostBot.Skills
{
    public class TeamsSkill : SkillDefinition
    {
        private const string SkillActionTeamsTaskModule = "TeamsTaskModule";
        private const string SkillActionTeamsCardAction = "TeamsCardAction";
        private const string SkillActionTeamsConversation = "TeamsConversation";
        private const string SkillActionTeamsCards = "Cards";
        private const string SkillActionTeamsProactive = "Proactive";
        private const string SkillActionTeamsAttachment = "Attachment";
        private const string SkillActionTeamsAuth = "Auth";
        private const string SkillActionTeamsSso = "Sso";
        private const string SkillActionTeamsEcho = "Echo";
        private const string SkillActionTeamsFileUpload = "FileUpload";

        public override IList<string> GetActions()
        {
            return new List<string>
            {
                SkillActionTeamsTaskModule,
                SkillActionTeamsCardAction,
                SkillActionTeamsConversation,
                SkillActionTeamsCards,
                SkillActionTeamsProactive,
                SkillActionTeamsAttachment,
                SkillActionTeamsAuth,
                SkillActionTeamsSso,
                SkillActionTeamsEcho,
                SkillActionTeamsFileUpload,
            };
        }

        public override Activity CreateBeginActivity(string actionId)
        {
            Activity activity;

            if (actionId.Equals(SkillActionTeamsTaskModule, StringComparison.CurrentCultureIgnoreCase))
            {
                activity = (Activity)Activity.CreateEventActivity();
                activity.Name = SkillActionTeamsTaskModule;
                return activity;
            }

            if (actionId.Equals(SkillActionTeamsCardAction, StringComparison.CurrentCultureIgnoreCase))
            {
                activity = (Activity)Activity.CreateEventActivity();
                activity.Name = SkillActionTeamsCardAction;
                return activity;
            }

            if (actionId.Equals(SkillActionTeamsConversation, StringComparison.CurrentCultureIgnoreCase))
            {
                activity = (Activity)Activity.CreateEventActivity();
                activity.Name = SkillActionTeamsConversation;
                return activity;
            }

            if (actionId.Equals(SkillActionTeamsCards, StringComparison.CurrentCultureIgnoreCase))
            {
                activity = (Activity)Activity.CreateEventActivity();
                activity.Name = SkillActionTeamsCards;
                return activity;
            }

            if (actionId.Equals(SkillActionTeamsProactive, StringComparison.CurrentCultureIgnoreCase))
            {
                activity = (Activity)Activity.CreateEventActivity();
                activity.Name = SkillActionTeamsProactive;
                return activity;
            }

            if (actionId.Equals(SkillActionTeamsAttachment, StringComparison.CurrentCultureIgnoreCase))
            {
                activity = (Activity)Activity.CreateEventActivity();
                activity.Name = SkillActionTeamsAttachment;
                return activity;
            }

            if (actionId.Equals(SkillActionTeamsAuth, StringComparison.CurrentCultureIgnoreCase))
            {
                activity = (Activity)Activity.CreateEventActivity();
                activity.Name = SkillActionTeamsAuth;
                return activity;
            }

            if (actionId.Equals(SkillActionTeamsSso, StringComparison.CurrentCultureIgnoreCase))
            {
                activity = (Activity)Activity.CreateEventActivity();
                activity.Name = SkillActionTeamsSso;
                return activity;
            }

            if (actionId.Equals(SkillActionTeamsEcho, StringComparison.CurrentCultureIgnoreCase))
            {
                activity = (Activity)Activity.CreateEventActivity();
                activity.Name = SkillActionTeamsEcho;
                return activity;
            }

            if (actionId.Equals(SkillActionTeamsFileUpload, StringComparison.CurrentCultureIgnoreCase))
            {
                activity = (Activity)Activity.CreateEventActivity();
                activity.Name = SkillActionTeamsFileUpload;
                return activity;
            }

            throw new InvalidOperationException($"Unable to create begin activity for \"{actionId}\".");
        }
    }
}
