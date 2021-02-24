// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { MessageFactory, InputHints } = require('botbuilder');
const { ComponentDialog, ChoicePrompt, WaterfallDialog, ChoiceFactory, DialogTurnStatus } = require('botbuilder-dialogs');
const fs = require('fs');
const path = require('path');

const ATTACHMENT_TYPE_PROMPT = 'AttachmentTypePrompt';
const END_PROMPT = 'EndPrompt';
const WATERFALL_DIALOG = 'WaterfallDialog';

class MessageWithAttachmentDialog extends ComponentDialog {
    /**
     * @param {string} dialogId
     */
    constructor(dialogId) {
        super(dialogId);

        this.picture = 'architecture-resize.png';

        this.addDialog(new ChoicePrompt(ATTACHMENT_TYPE_PROMPT))
            .addDialog(new ChoicePrompt(END_PROMPT))
            .addDialog(new WaterfallDialog(WATERFALL_DIALOG, [
                this.selectAttachment.bind(this),
                this.handleAttachment.bind(this),
                this.finalStep.bind(this)
            ]));

        this.initialDialogId = WATERFALL_DIALOG;
    }

    /**
     * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
     */
    async selectAttachment(stepContext) {
        // Create the PromptOptions from the skill configuration which contain the list of configured skills.
        const messageText = 'What card do you want?';
        const repromptMessageText = 'That was not a valid choice, please select a valid card type.';
        const options = {
            prompt: MessageFactory.text(messageText, messageText, InputHints.ExpectingInput),
            retryPrompt: MessageFactory.text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
            choices: ChoiceFactory.toChoices(['Inline', 'Internet'])

            // This is currently excluded since Attachments endpoint isn't currently implemented in the ChannelServiceHandler
            // choices: ['Upload']
        };

        return stepContext.prompt(ATTACHMENT_TYPE_PROMPT, options);
    }

    /**
     * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
     */
    async handleAttachment(stepContext) {
        const card = stepContext.context.activity.text;
        const reply = MessageFactory.text('');

        switch (card) {
        case 'Inline':
            reply.text = 'This is an inline attachment.';
            reply.attachments = [this.getInlineAttachment()];
            break;

        case 'Internet':
            reply.text = 'This is an attachment from a HTTP URL.';
            reply.attachments = [this.getInternetAttachment()];
            break;

        case 'Upload':
            // Commenting this out since the Attachments endpoint isn't currently implemented in the ChannelService Handler

            // reply.text = 'This is an uploaded attachment.';
            // Get the uploaded attachment.
            // var uploadedAttachment = await this.getUploadedAttachment(stepContext, stepContext.context.activity.serviceUrl, stepContext.context.activity.conversation.id);
            // reply.attachments = [uploadedAttachment];
            break;

        default:
            reply.text = 'Invalid choice';
            break;
        }

        await stepContext.context.sendActivity(reply);

        const messageText = 'Do you want another type of attachment?';
        const repromptMessageText = "That's an invalid choice.";

        return stepContext.prompt(END_PROMPT, {
            prompt: MessageFactory.text(messageText, messageText, InputHints.ExpectingInput),
            retryPrompt: MessageFactory.text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
            choices: ChoiceFactory.toChoices(['Yes', 'No'])
        });
    }

    /**
     * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
     */
    async finalStep(stepContext) {
        const selectedChoice = stepContext.result.value.toLowerCase();

        if (selectedChoice === 'yes') {
            return stepContext.replaceDialog(this.initialDialogId);
        }

        return { status: DialogTurnStatus.complete };
    }

    /**
     * Returns an inline attachment.
     * @returns {import('botbuilder').Attachment}
     */
    getInlineAttachment() {
        const filepath = path.resolve(__dirname, 'files', this.picture);
        const file = fs.readFileSync(filepath, 'base64');
        return {
            name: `Files\\${ this.picture }`,
            contentType: 'image/png',
            contentUrl: `data:image/png;base64,${ file }`
        };
    }

    /**
     * Returns an attachment to be sent to the user from a HTTPS URL.
     * @returns {import('botbuilder').Attachment}
     */
    getInternetAttachment() {
        return {
            name: `Files\\${ this.picture }`,
            contentType: 'image/png',
            contentUrl: 'https://docs.microsoft.com/en-us/bot-framework/media/how-it-works/architecture-resize.png'
        };
    }

    /**
     * Returns an attachment that has been uploaded to the channel's blob storage.
     * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
     * @param {string} serviceUrl
     * @param {string} conversationId
     * @returns {import('botbuilder').Attachment}
     */
    async getUploadedAttachment(stepContext, serviceUrl, conversationId) {
        if (!serviceUrl) {
            throw new Error("Argument 'serviceUrl' is null or empty.");
        }

        if (!conversationId) {
            throw new Error("Argument 'conversationId' is null or empty.");
        }

        const imagePath = path.resolve(__dirname, 'files', this.picture);
        const image = fs.readFileSync(imagePath);

        const connector = stepContext.context.turnState.get(stepContext.context.adapter.ConnectorClientKey);
        const response = await connector.conversations.uploadAttachment(conversationId, {
            name: `Files\\${ this.picture }`,
            type: 'image/png',
            originalBase64: image
        });

        const baseUri = connector.baseUri;
        const attachmentUri = baseUri + (baseUri.endsWith('/') ? '' : '/') + `v3/attachments/${ encodeURI(response.id) }/views/original`;

        return {
            name: `Files\\${ this.picture }`,
            contentType: 'image/png',
            contentUrl: attachmentUri
        };
    }
}

module.exports.MessageWithAttachmentDialog = MessageWithAttachmentDialog;
