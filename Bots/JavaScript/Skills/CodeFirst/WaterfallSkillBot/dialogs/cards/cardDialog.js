// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { InputHints, MessageFactory } = require('botbuilder');
const { ComponentDialog, WaterfallDialog, ChoicePrompt, ListStyle, ChoiceFactory, DialogTurnStatus } = require('botbuilder-dialogs');
const fs = require('fs');
const path = require('path');
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
  constructor (dialogId, serverUrl) {
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
  async selectCard (stepContext) {
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
  async displayCard (stepContext) {
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

        case CardOptions.AdaptiveCardSubmitAction:
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
          await stepContext.context.sendActivity(MessageFactory.attachment(CardSampleHelper.createAudioCard(`${this.serverUrl}/api/music`)));
          break;

        case CardOptions.Video:
          await stepContext.context.sendActivity(MessageFactory.attachment(CardSampleHelper.createVideoCard(this.corgiOnCarouselVideo)));
          break;

        case CardOptions.End:
          return { status: DialogTurnStatus.complete };
      }
    } else {
      await stepContext.context.sendActivity(MessageFactory.text(`${card} cards are not supported in the ${channelId} channel.`));
    }

    return stepContext.replaceDialog(this.initialDialogId);
  }

  /**
     * @param {import('botbuilder-dialogs').PromptValidatorContext<import('botbuilder-dialogs').FoundChoice>} promptContext
     */
  async cardPromptValidator (promptContext) {
    if (!promptContext.recognized.succeeded) {
      if (promptContext.context.activity.attachments) {
        return true;
      }

      const activityJson = JSON.stringify(promptContext.context.activity, null, 4).replace(/\n/g, '\r\n');

      // Render the activity so we can assert in tests.
      // We may need to simplify the json if it gets too complicated to test.
      promptContext.options.retryPrompt.text = `Got ${activityJson}\n\n${promptContext.options.prompt.text}`;
      return false;
    }

    return true;
  }
}

module.exports.CardDialog = CardDialog;
