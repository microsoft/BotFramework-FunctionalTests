// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { InputHints, MessageFactory,} = require('botbuilder');
const { ComponentDialog, WaterfallDialog } = require('botbuilder-dialogs');

const TANGENT_DIALOG = 'TangentDialog';
const WATERFALL_DIALOG = 'WaterfallDialog';
const TEXT_PROMPT = 'TextPrompt';

class TangentDialog extends ComponentDialog {
    constructor(dialogId = TANGENT_DIALOG) {
        super(dialogId);

        this.addDialog(new WaterfallDialog(WATERFALL_DIALOG, [
            this.step1.bind(this),
            this.step2.bind(this),
            this.endStep.bind(this)
        ]));

        this.initialDialogId = WATERFALL_DIALOG;
    }

    async step1(stepContext) {
        const messageText = 'Tangent step 1 of 2, say something.';
        const promptMessage = MessageFactory(messageText, messageText, InputHints.ExpectingInput);

        return await stepContext.prompt(TEXT_PROMPT, { prompt: promptMessage });
    }

    async step2(stepContext) {
        const messageText = 'Tangent step 2 of 2, say something.';
        const promptMessage = MessageFactory(messageText, messageText, InputHints.ExpectingInput);

        return await stepContext.prompt(TEXT_PROMPT, { prompt: promptMessage });
    }

    async endStep(stepContext) {
        return await stepContext.endDialog();
    }
}

module.exports.TangentDialog = TangentDialog;
