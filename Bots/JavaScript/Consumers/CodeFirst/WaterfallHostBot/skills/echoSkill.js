// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { Activity } = require('botbuilder');
const SkillActionMessage = 'Message';

class EchoSkill {
    getActions() {
        return SkillActionMessage;
    }

    createBeginActivity(actionId) {
        if (actionId === SkillActionMessage) {
            let activity = Activity.createMessageActivity();
            activity.name = SkillActionMessage;
            activity.text = 'Begin the Echo Skill.';

            return activity;
        }

        throw new Error(`[EchoSkill]: Unable to create begin activity for \"${ actionId }\".`);
    }
}

module.exports.EchoSkill = EchoSkill;
