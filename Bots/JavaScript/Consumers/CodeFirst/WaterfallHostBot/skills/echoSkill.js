// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ActivityEx } = require('botbuilder-core');
const { SkillDefinition } = require('./skillDefinition');

const SKILL_ACTION_MESSAGE = 'Message';

class EchoSkill extends SkillDefinition {
    getActions() {
        return [SKILL_ACTION_MESSAGE];
    }

    /**
     * @param {string} actionId
     */
    createBeginActivity(actionId) {
        const activity = ActivityEx.createMessageActivity();

        switch (actionId) {
        case SKILL_ACTION_MESSAGE:
            activity.name = SKILL_ACTION_MESSAGE;
            activity.text = 'Begin the Echo Skill.';
            break;

        default:
            throw new Error(`[EchoSkill]: Unable to create begin activity for "${ actionId }".`);
        }

        return activity;
    }
}

module.exports.EchoSkill = EchoSkill;
