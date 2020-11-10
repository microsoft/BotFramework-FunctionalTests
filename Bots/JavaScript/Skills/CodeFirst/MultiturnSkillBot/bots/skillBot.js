// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ActivityHandler } = require('botbuilder');
const { runDialog } = require('botbuilder-dialogs');

class SkillBot extends ActivityHandler {
    /**
     *
     * @param {ConversationState} conversationState
     * @param {Dialog} dialog
     */
    constructor(conversationState, dialog) {
        super();
        if (!conversationState) throw new Error('[SkillBot]: Missing parameter. conversationState is required');
        if (!dialog) throw new Error('[SkillBot]: Missing parameter. dialog is required');

        this.conversationState = conversationState;
        this.dialog = dialog;

        this.onTurn(async (context, next) => {
            await runDialog(this.dialog, context, this.conversationState.createProperty('DialogState'));

            await next();
        });
    }

    /**
     * Override the ActivityHandler.run() method to save state changes after the bot logic completes.
     */
    async run(context) {
        await super.run(context);

        // Save any state changes. The load happened during the execution of the Dialog.
        await this.conversationState.saveChanges(context, false);
    }
}

module.exports.SkillBot = SkillBot;
