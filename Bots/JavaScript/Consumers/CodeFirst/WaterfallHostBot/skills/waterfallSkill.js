// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ActivityEx } = require('botbuilder-core');
const { SkillDefinition } = require('./skillDefinition');

const SKILL_ACTION_CARDS = 'Cards';
const SKILL_ACTION_PROACTIVE = 'Proactive';
const SKILL_ACTION_AUTH = 'Auth';
const SKILL_ACTION_MESSAGE_WITH_ATTACHMENT = 'MessageWithAttachment';
const SKILL_ACTION_SSO = 'Sso';
const SKILL_ACTION_FILE_UPLOAD = 'FileUpload';
const SKILL_ACTION_CALL_ECHO_SKILL = 'Echo';
const SKILL_ACTION_CALL_DELETE_SKILL = 'Delete';
const SKILL_ACTION_CALL_UPDATE_SKILL = 'Update';

class WaterfallSkill extends SkillDefinition {
    getActions() {
        return [
            SKILL_ACTION_CARDS,
            SKILL_ACTION_PROACTIVE,
            SKILL_ACTION_AUTH,
            SKILL_ACTION_MESSAGE_WITH_ATTACHMENT,
            SKILL_ACTION_SSO,
            SKILL_ACTION_FILE_UPLOAD,
            SKILL_ACTION_CALL_ECHO_SKILL,
            SKILL_ACTION_CALL_DELETE_SKILL,
            SKILL_ACTION_CALL_UPDATE_SKILL,
        ];
    }

    /**
     * @param {string} actionId
     */
    createBeginActivity(actionId) {
        const activity = ActivityEx.createEventActivity();

        switch (actionId) {
        // Send an event activity to the skill with "Cards" in the name.
        case SKILL_ACTION_CARDS:
            activity.name = SKILL_ACTION_CARDS;
            break;

            // Send an event activity to the skill with "Proactive" in the name.
        case SKILL_ACTION_PROACTIVE:
            activity.name = SKILL_ACTION_PROACTIVE;
            break;

            // Send an event activity to the skill with "Auth" in the name.
        case SKILL_ACTION_AUTH:
            activity.name = SKILL_ACTION_AUTH;
            break;

            // Send an event activity to the skill with "Attachment" in the name.
        case SKILL_ACTION_MESSAGE_WITH_ATTACHMENT:
            activity.name = SKILL_ACTION_MESSAGE_WITH_ATTACHMENT;
            break;

            // Send an event activity to the skill with "Sso" in the name.
        case SKILL_ACTION_SSO:
            activity.name = SKILL_ACTION_SSO;
            break;

            // Send an event activity to the skill with "FileUpload" in the name.
        case SKILL_ACTION_FILE_UPLOAD:
            activity.name = SKILL_ACTION_FILE_UPLOAD;
            break;

            // Send an event activity to the skill with "Echo" in the name.
        case SKILL_ACTION_CALL_ECHO_SKILL:
            activity.name = SKILL_ACTION_CALL_ECHO_SKILL;
            break;
        
        case SKILL_ACTION_CALL_DELETE_SKILL:
            activity.name = SKILL_ACTION_CALL_DELETE_SKILL;
            break;

        case SKILL_ACTION_CALL_UPDATE_SKILL:
            activity.name = SKILL_ACTION_CALL_UPDATE_SKILL;
            break;

        default:
            throw new Error(`[WaterfallSkill]: Unable to create begin activity for "${ actionId }".`);
        }

        return activity;
    }
}

module.exports.WaterfallSkill = WaterfallSkill;