// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ActivityEx } = require('botbuilder-core');
const { SkillDefinition } = require('./skillDefinition');

const SKILL_ACTION_TEAMS_TASK_MODULE = 'TeamsTaskModule';
const SKILL_ACTION_TEAMS_CARD_ACTION = 'TeamsCardAction';
const SKILL_ACTION_TEAMS_CONVERSATION = 'TeamsConversation';

class TeamsSkill extends SkillDefinition {
    getActions() {
        return [
            SKILL_ACTION_TEAMS_TASK_MODULE,
            SKILL_ACTION_TEAMS_CARD_ACTION,
            SKILL_ACTION_TEAMS_CONVERSATION
        ];
    }

    /**
     * @param {string} actionId
     */
    createBeginActivity(actionId) {
        const activity = ActivityEx.createEventActivity();

        switch (actionId) {
        case SKILL_ACTION_TEAMS_TASK_MODULE:
            activity.name = SKILL_ACTION_TEAMS_TASK_MODULE;
            break;

        case SKILL_ACTION_TEAMS_CARD_ACTION:
            activity.name = SKILL_ACTION_TEAMS_CARD_ACTION;
            break;

        case SKILL_ACTION_TEAMS_CONVERSATION:
            activity.name = SKILL_ACTION_TEAMS_CONVERSATION;
            break;

        default:
            throw new Error(`[TeamsSkill]: Unable to create begin activity for "${ actionId }".`);
        }

        return activity;
    }
}

module.exports.TeamsSkill = TeamsSkill;
