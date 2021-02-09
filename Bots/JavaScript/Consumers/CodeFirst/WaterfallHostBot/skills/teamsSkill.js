// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ActivityEx } = require('botbuilder-schema');
const SKILL_ACTION_TEAMS_TASK_MODULE = 'TeamsTaskModule';
const SKILL_ACTION_TEAMS_CARD_ACTION = 'TeamsCardAction';
const SKILL_ACTION_TEAMS_CONVERSATION = 'TeamsConversation';


class TeamsSkill {
    getActions() {
        return [
            SKILL_ACTION_TEAMS_TASK_MODULE,
            SKILL_ACTION_TEAMS_CARD_ACTION,
            SKILL_ACTION_TEAMS_CONVERSATION
        ];
    }

    createBeginActivity(actionId) {
        if (actionId === SKILL_ACTION_TEAMS_TASK_MODULE) {
            let activity = ActivityEx.createEventActivity();
            activity.name = SKILL_ACTION_TEAMS_TASK_MODULE;
            return activity;
        }

        if (actionId === SKILL_ACTION_TEAMS_CARD_ACTION) {
            let activity = ActivityEx.createEventActivity();
            activity.name = SKILL_ACTION_TEAMS_CARD_ACTION;
            return activity;
        }

        if (actionId === SKILL_ACTION_TEAMS_CONVERSATION) {
            let activity = ActivityEx.createEventActivity();
            activity.name = SKILL_ACTION_TEAMS_CONVERSATION;
            return activity;
        }

        throw new Error(`[TeamsSkill]: Unable to create begin activity for \"${ actionId }\".`);
    }
}

module.exports.TeamsSkill = TeamsSkill;
