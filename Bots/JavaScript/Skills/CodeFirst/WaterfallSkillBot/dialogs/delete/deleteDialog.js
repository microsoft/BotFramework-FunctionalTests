// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { MessageFactory, InputHints } = require('botbuilder');
const { ChoiceFactory, ChoicePrompt, ComponentDialog, WaterfallDialog, DialogTurnStatus, ListStyle } = require('botbuilder-dialogs');
const { Channels, TurnContext } = require('botbuilder-core');

const SLEEP_TIMER = 5000;
const WATERFALL_DIALOG = 'WaterfallDialog';
const CHOICE_PROMPT = 'ChoicePrompt';
const DELETE_UNSUPPORTED = new Set([Channels.Emulator, Channels.Facebook, Channels.Webchat]);

class DeleteDialog extends ComponentDialog {
    /**
     * @param {string} dialogId
     */
    constructor(dialogId) {
        super(dialogId);

        this.addDialog(new ChoicePrompt(CHOICE_PROMPT))
            .addDialog(new WaterfallDialog(WATERFALL_DIALOG, [
                this.HandleDeleteDialog.bind(this),
                this.FinalStep.bind(this)
            ]));

        this.initialDialogId = WATERFALL_DIALOG;
    }

    /**
     * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
     */
    async HandleDeleteDialog(stepContext) {
        let channel = stepContext.context.activity.channelId;

        if (!DeleteDialog.isDeleteSupported(channel))
        {
            var activity = MessageFactory.text("I will delete this message in 5 seconds");
            TurnContext.applyConversationReference(activity, TurnContext.getConversationReference(stepContext.context.activity));
            var id = await stepContext.context.sendActivity(activity);
            activity.id = id.id;
            await DeleteDialog.sleep(SLEEP_TIMER);
            await stepContext.context.deleteActivity(TurnContext.getConversationReference(activity));
        }
        else
        {
            await stepContext.context.sendActivity(MessageFactory.text(`Delete is not supported in the ${channel} channel.`))
        }    

        const messageText = 'Do you want to delete again?';
        const repromptMessageText = 'You must select "Yes" or "No".';

        return stepContext.prompt(CHOICE_PROMPT, {
            prompt: MessageFactory.text(messageText, messageText, InputHints.ExpectingInput),
            retryPrompt: MessageFactory.text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
            choices: ChoiceFactory.toChoices(['Yes', 'No']),
            style: ListStyle.list
        });
    }

    async FinalStep(stepContext) {
        const choice = stepContext.result.value.toLowerCase();

        if (choice === 'yes') {
            return stepContext.replaceDialog(this.initialDialogId);
        } else {
            return { status: DialogTurnStatus.complete };
        }
    }

    static sleep(milliseconds) {
        return new Promise(resolve => {
            setTimeout(resolve, milliseconds);
        });
    }

    static isDeleteSupported(channel) {
        return !DELETE_UNSUPPORTED.has(channel);
    }
}

module.exports.DeleteDialog = DeleteDialog
