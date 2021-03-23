// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ActionTypes, CardFactory, InputHints, MessageFactory } = require('botbuilder');
const { ComponentDialog, WaterfallDialog, ChoicePrompt, ListStyle, ChoiceFactory, DialogTurnStatus } = require('botbuilder-dialogs');
const fs = require('fs');
const path = require('path');
const { title } = require('process');
const { stringify } = require('querystring');
const { CardOptions } = require('./cardOptions');
const { CardSampleHelper } = require('./cardSampleHelper');
const { ChannelSupportedCards } = require('./channelSupportedCards');

const WATERFALL_DIALOG = 'WaterfallDialog';
const CARD_PROMPT = 'CardPrompt';

class CardDialog extends ComponentDialog {
    /**
     * @param {string} dialogId
     * @param {string} serverUrl
     */
    constructor(dialogId, serverUrl) {
        super(dialogId);
        this.serverUrl = serverUrl;
        this.mindBlownGif = 'https://media3.giphy.com/media/xT0xeJpnrWC4XWblEk/giphy.gif?cid=ecf05e47mye7k75sup6tcmadoom8p1q8u03a7g2p3f76upp9&rid=giphy.gif';
        this.corgiOnCarouselVideo = 'https://www.youtube.com/watch?v=LvqzubPZjHE';
        this.teamsLogoFileName = 'teams-logo.png';

        this.addDialog(new ChoicePrompt(CARD_PROMPT, this.cardPromptValidator))
            .addDialog(new WaterfallDialog(WATERFALL_DIALOG, [
                this.selectCard.bind(this),
                this.displayCard.bind(this)
            ]));

        this.initialDialogId = WATERFALL_DIALOG;
    }

    /**
     * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
     */
    async selectCard(stepContext) {
        // Create the PromptOptions from the skill configuration which contain the list of configured skills.
        const messageText = 'What card do you want?';
        const repromptMessageText = 'This message will be created in the validation code';
        const options = {
            prompt: MessageFactory.text(messageText, messageText, InputHints.ExpectingInput),
            retryPrompt: MessageFactory.text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
            choices: ChoiceFactory.toChoices(Object.values(CardOptions)),
            style: ListStyle.list
        };

        // Ask the user to enter a card choice.
        return stepContext.prompt(CARD_PROMPT, options);
    }

    /**
     * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
     */
    async displayCard(stepContext) {
        if (stepContext.context.activity.value){
            console.log('#####################');
            console.log(stepContext.context.activity);
            await this.handleAdaptiveCard(stepContext);
        }
        else
        {
            const card = stepContext.result.value;
            const cardType = Object.keys(CardOptions).find(key => CardOptions[key].toLowerCase() === card.toLowerCase());
            const { channelId } = stepContext.context.activity;
    
            if (ChannelSupportedCards.isCardSupported(channelId, cardType)) {
                switch (cardType) {
                case CardOptions.AdaptiveCardBotAction:
                    await stepContext.context.sendActivity(MessageFactory.attachment(CardSampleHelper.createAdaptiveCardBotAction()));
                    break;
    
                case CardOptions.AdaptiveCardTaskModule:
                    await stepContext.context.sendActivity(MessageFactory.attachment(CardSampleHelper.createAdaptiveCardTaskModule()));
                    break;
    
                case CardOptions.AdaptiveCardSumbitAction:
                    await stepContext.context.sendActivity(MessageFactory.attachment(CardSampleHelper.createAdaptiveCardSubmit()));
                    break;
    
                case CardOptions.Hero:
                    await stepContext.context.sendActivity(MessageFactory.attachment(CardSampleHelper.createHeroCard()));
                    break;
    
                case CardOptions.Thumbnail:
                    await stepContext.context.sendActivity(MessageFactory.attachment(CardSampleHelper.createThumbnailCard()));
                    break;
    
                case CardOptions.Receipt:
                    await stepContext.context.sendActivity(MessageFactory.attachment(CardSampleHelper.createReceiptCard()));
                    break;
    
                case CardOptions.Signin:
                    await stepContext.context.sendActivity(MessageFactory.attachment(CardSampleHelper.createSigninCard()));
                    break;
    
                case CardOptions.Carousel:
                    // NOTE: if cards are NOT the same height in a carousel, Teams will instead display as AttachmentLayoutTypes.List
                    await stepContext.context.sendActivity(MessageFactory.carousel([
                        CardSampleHelper.createHeroCard(),
                        CardSampleHelper.createHeroCard(),
                        CardSampleHelper.createHeroCard()
                    ]));
                    break;
    
                case CardOptions.List:
                    await stepContext.context.sendActivity(MessageFactory.list([
                        CardSampleHelper.createHeroCard(),
                        CardSampleHelper.createHeroCard(),
                        CardSampleHelper.createHeroCard()
                    ]));
                    break;
    
                case CardOptions.O365:
                    await stepContext.context.sendActivity(MessageFactory.attachment(CardSampleHelper.createO365ConnectorCard()));
                    break;
    
                case CardOptions.TeamsFileConsent: {
                    const file = fs.readFileSync(path.resolve(__dirname, 'files', this.teamsLogoFileName));
                    await stepContext.context.sendActivity(MessageFactory.attachment(CardSampleHelper.createTeamsFileConsentCard(this.teamsLogoFileName, file.byteLength)));
                    break;
                }
    
                case CardOptions.Animation:
                    await stepContext.context.sendActivity(MessageFactory.attachment(CardSampleHelper.createAnimationCard(this.mindBlownGif)));
                    break;
    
                case CardOptions.Audio:
                    await stepContext.context.sendActivity(MessageFactory.attachment(CardSampleHelper.createAudioCard(`${ this.serverUrl }/api/music`)));
                    break;
    
                case CardOptions.Video:
                    await stepContext.context.sendActivity(MessageFactory.attachment(CardSampleHelper.createVideoCard(this.corgiOnCarouselVideo)));
                    break;
                
                case CardOptions.AdaptiveUpdate:
                    var id = await stepContext.context.sendActivity(MessageFactory.attachment(CardDialog.MakeAdaptiveUpdateCard()));
                    console.log(`##### this update card id is ${id.id}`);
                    break;
    
                case CardOptions.End:
                    return { status: DialogTurnStatus.complete };
                }
            } else {
                await stepContext.context.sendActivity(MessageFactory.text(`${ card } cards are not supported in the ${ channelId } channel.`));
            }
        }
        

        return stepContext.replaceDialog(this.initialDialogId);
    }

    static MakeAdaptiveUpdateCard(){
        const { title, text,  buttons } = {
            title: 'Adaptive Update Card',
            text: 'Update Card Action',
            buttons: [
                {
                    type: ActionTypes.MessageBack,
                    title: 'Update card',
                    value: {'count': 0}
                }
            ]
        };

        return CardFactory.heroCard(title, null, buttons, { text });
    }

    /**
     * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
     */
    async handleAdaptiveCard(stepContext){
        if (stepContext.context.activity.text === undefined)
        {
            await stepContext.context.sendActivity(MessageFactory.text(`I received an activity with this data in the value field ${JSON.stringify(stepContext.context.activity.value)} `))
        }
        else
        {
            if (stepContext.context.activity.text.includes('update')){
                if (stepContext.context.activity.replyToId){
                    var updatedActivity = MessageFactory.attachment(CardDialog.MakeUpdatedHeroCard(stepContext));
                    console.log(`The bumped card activity id is ${stepContext.context.activity.replyToId}`);
                    updatedActivity.id = stepContext.context.activity.replyToId;
                    await stepContext.context.updateActivity(updatedActivity);
                }
                else
                {
                    await stepContext.context.sendActivity(MessageFactory.text(`Update activity is not supported in the ${stepContext.context.activity.channelId} channel.`));
                }
            }
            else if (stepContext.context.activity.text === ActionTypes.ImBack){
                await stepContext.context.sendActivity(MessageFactory.text("You clicked the imBack button the adaptive card"));
            }
            else
            {
                await stepContext.context.sendActivity(MessageFactory.text(`I received an activity with this data in the text field ${JSON.stringify(stepContext.context.activity.text)} and this data in the value field ${JSON.stringify(stepContext.context.activity.value)}`));
            }
        }
    }

    /**
     * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
     */
    static MakeUpdatedHeroCard(stepContext){
        var data = stepContext.context.activity.value;
        data['count'] += 1;

        const { title, text,  buttons } = {
            title: 'Nearly updated card',
            text: `Update count - ${data['count']}`,
            buttons: [
                {
                    type: ActionTypes.MessageBack,
                    title: 'Update card',
                    text: 'Update card text',
                    value: data
                }
            ]
        };

        return CardFactory.heroCard(title, null, buttons, { text });
    }

    /**
     * @param {import('botbuilder-dialogs').PromptValidatorContext<import('botbuilder-dialogs').FoundChoice>} promptContext
     */
    async cardPromptValidator(promptContext) {
        if (!promptContext.recognized.succeeded) {
                // for attachments                             // for adaptive cards                  // for messages from cards
            if (promptContext.context.activity.attachments || promptContext.context.activity.value || promptContext.context.activity.text) {
                return true;
            }

            const activityJson = JSON.stringify(promptContext.context.activity, null, 4).replace(/\n/g, '\r\n');

            // Render the activity so we can assert in tests.
            // We may need to simplify the json if it gets too complicated to test.
            promptContext.options.retryPrompt.text = `Got ${ activityJson }\n\n${ promptContext.options.prompt.text }`;
            return false;
        }

        return true;
    }
}

module.exports.CardDialog = CardDialog;
