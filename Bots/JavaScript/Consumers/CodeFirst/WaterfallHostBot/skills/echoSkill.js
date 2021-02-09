// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ActivityEx } = require('botbuilder-core');
const { SkillDefinition } = require('./skillDefinition');

const SKILL_ACTION_MESSAGE = 'Message';

class EchoSkill extends SkillDefinition {
    getActions() {
        return SKILL_ACTION_MESSAGE;
    }

    createBeginActivity(actionId) {
        if (actionId === SKILL_ACTION_MESSAGE) {
            let activity = ActivityEx.createMessageActivity();
            activity.name = SKILL_ACTION_MESSAGE;
            activity.text = 'Begin the Echo Skill.';
            return activity;
        }

        throw new Error(`[EchoSkill]: Unable to create begin activity for \"${ actionId }\".`);
    }
}

module.exports.EchoSkill = EchoSkill;
