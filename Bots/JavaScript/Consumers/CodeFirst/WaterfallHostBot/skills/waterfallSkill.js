// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ActivityEx } = require('botbuilder-schema');
const SKILL_ACTION_CARDS = 'Cards';
const SKILL_ACTION_PROACTIVE = 'Proactive';
const SKILL_ACTION_AUTH = 'Auth';
const SKILL_ACTION_MESSAGE_WITH_ATTACHMENT = 'MessageWithAttachment';
const SKILL_ACTION_SSO = 'Sso';
const SKILL_ACTION_FILE_UPLOAD = 'FileUpload';
const SKILL_ACTION_CALL_ECHO_SKILL = 'Echo';

class WaterfallSkill {
    getActions() {
        return [
            SKILL_ACTION_CARDS,
            SKILL_ACTION_PROACTIVE,
            SKILL_ACTION_AUTH,
            SKILL_ACTION_MESSAGE_WITH_ATTACHMENT,
            SKILL_ACTION_SSO,
            SKILL_ACTION_FILE_UPLOAD,
            SKILL_ACTION_CALL_ECHO_SKILL
        ];
    }

    createBeginActivity(actionId) {
        // Send an event activity to the skill with "Cards" in the name.
        if (actionId === SKILL_ACTION_CARDS) {
            let activity = ActivityEx.createEventActivity();
            activity.name = SKILL_ACTION_CARDS;
            return activity;
        }
        // Send an event activity to the skill with "Proactive" in the name.
        if (actionId === SKILL_ACTION_PROACTIVE) {
            let activity = ActivityEx.createEventActivity();
            activity.name = SKILL_ACTION_PROACTIVE;
            return activity;
        }
        // Send an event activity to the skill with "Auth" in the name.
        if (actionId === SKILL_ACTION_AUTH) {
            let activity = ActivityEx.createEventActivity();
            activity.name = SKILL_ACTION_AUTH;
            return activity;
        }
        // Send an event activity to the skill with "Attachment" in the name.
        if (actionId === SKILL_ACTION_MESSAGE_WITH_ATTACHMENT) {
            let activity = ActivityEx.createEventActivity();
            activity.name = SKILL_ACTION_MESSAGE_WITH_ATTACHMENT;
            return activity;
        }
        // Send an event activity to the skill with "Sso" in the name.
        if (actionId === SKILL_ACTION_SSO) {
            let activity = ActivityEx.createEventActivity();
            activity.name = SKILL_ACTION_SSO;
            return activity;
        }
        // Send an event activity to the skill with "FileUpload" in the name.
        if (actionId === SKILL_ACTION_FILE_UPLOAD) {
            let activity = ActivityEx.createEventActivity();
            activity.name = SKILL_ACTION_FILE_UPLOAD;
            return activity;
        }
        // Send an event activity to the skill with "Echo" in the name.
        if (actionId === SKILL_ACTION_CALL_ECHO_SKILL) {
            let activity = ActivityEx.createEventActivity();
            activity.name = SKILL_ACTION_CALL_ECHO_SKILL;
            return activity;
        }

        throw new Error(`[WaterfallSkill]: Unable to create begin activity for \"${ actionId }\".`);
    }
}

module.exports.WaterfallSkill = WaterfallSkill;
