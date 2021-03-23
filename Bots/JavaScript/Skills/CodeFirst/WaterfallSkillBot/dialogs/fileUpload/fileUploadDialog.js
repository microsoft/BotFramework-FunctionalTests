// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { MessageFactory, InputHints, Channels } = require('botbuilder');
const { ComponentDialog, AttachmentPrompt, ChoicePrompt, WaterfallDialog, ListStyle, ChoiceFactory, DialogTurnStatus } = require('botbuilder-dialogs');
const fs = require('fs');
const fetch = require('node-fetch');
const os = require('os');
const path = require('path');
const stream = require('stream');
const util = require('util');

const streamPipeline = util.promisify(stream.pipeline);

const ATTACHMENT_PROMPT = 'AttachmentPrompt';
const CHOICE_PROMPT = 'ChoicePrompt';
const WATERFALL_DIALOG = 'WaterfallDialog';

class FileUploadDialog extends ComponentDialog {
    /**
     * @param {string} dialogId
     */
    constructor(dialogId) {
        super(dialogId);

        this.addDialog(new AttachmentPrompt(ATTACHMENT_PROMPT))
            .addDialog(new ChoicePrompt(CHOICE_PROMPT))
            .addDialog(new WaterfallDialog(WATERFALL_DIALOG, [
                this.promptUploadStep.bind(this),
                this.handleAttachmentStep.bind(this),
                this.finalStep.bind(this)
            ]));

        this.initialDialogId = WATERFALL_DIALOG;
    }

    /**
     * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
     */
    async promptUploadStep(stepContext) {
        return stepContext.prompt(ATTACHMENT_PROMPT, {
            prompt: MessageFactory.text('Please upload a file to continue.'),
            retryPrompt: MessageFactory.text('You must upload a file.')
        });
    }

    /**
     * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
     */
    async handleAttachmentStep(stepContext) {
        let filetext = '';

        for (const file of stepContext.context.activity.attachments) {
            console.log("#################");
            console.log(file);
            console.log(os.tmpdir());

            var fileName = '';
            if ([Channels.Telegram, Channels.Facebook].includes(stepContext.context.activity.channelId)){
                fileName = "temp.jpg";
            }
            else
            {
                fileName = file.name;
            }
            
            const localFileName = path.resolve(os.tmpdir(), fileName);
            const tempFile = fs.createWriteStream(localFileName);
            fetch(file.contentUrl).then(response => streamPipeline(response.body, tempFile));

            filetext += `Attachment "${ file.name }" has been received and saved to "${ localFileName }"\r\n`;
        }

        await stepContext.context.sendActivity(MessageFactory.text(filetext));

        const messageText = 'Do you want to upload another file?';
        const repromptMessageText = 'You must select "Yes" or "No".';

        return stepContext.prompt(CHOICE_PROMPT, {
            prompt: MessageFactory.text(messageText, messageText, InputHints.ExpectingInput),
            retryPrompt: MessageFactory.text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
            choices: ChoiceFactory.toChoices(['Yes', 'No']),
            style: ListStyle.list
        });
    }

    /**
     * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
     */
    async finalStep(stepContext) {
        const choice = stepContext.result.value.toLowerCase();

        if (choice === 'yes') {
            return stepContext.replaceDialog(this.initialDialogId);
        } else {
            return { status: DialogTurnStatus.complete };
        }
    }
}

module.exports.FileUploadDialog = FileUploadDialog;
