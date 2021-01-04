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

        public override IList<string> GetActions()
        {
            return new List<string>
            {
                SkillActionTeamsTaskModule,
                SkillActionTeamsCardAction,
                SkillActionTeamsConversation
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

            throw new InvalidOperationException($"Unable to create begin activity for \"{actionId}\".");
        }
    }
}
