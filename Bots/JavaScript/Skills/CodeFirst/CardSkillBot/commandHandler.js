// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { CardFactory, MessageFactory, teamsGetChannelId, TeamsInfo, TurnContext } = require('botbuilder');
const { CardSampleHelper } = require('./cardSampleHelper');
const Constants = require('./constants.js');
const path = require('path');
const fs = require('fs');

class CommandHandler {
    static async handleCommand(turnContext, actualText, bot) {
        switch (actualText.toLowerCase()) {
        case 'botactions':
            await CommandHandler.sendAdaptiveCard(turnContext, 'botactions');
            break;
        case 'taskmodule':
            await CommandHandler.sendAdaptiveCard(turnContext, 'taskmodule');
            break;
        case 'submitaction':
            await CommandHandler.sendAdaptiveCard(turnContext, 'submitaction');
            break;
        case 'hero':
            await turnContext.sendActivity(MessageFactory.attachment(CardSampleHelper.createHeroCard()));
            break;
        default:
            await turnContext.sendActivity(MessageFactory.text("Send me one of these messages for a card: botActions, taskModule, submitAction, hero."));
            break;
        }
    }

    static async sendAdaptiveCard(turnContext, cardType) {
        let card;
        switch (cardType) {
        case 'botactions':
            card = CardSampleHelper.createAdaptiveCard1();
            break;
        case 'taskmodule':
            card = CardSampleHelper.createAdaptiveCard2();
            break;
        case 'submitaction':
            card = CardSampleHelper.createAdaptiveCard3();
            break;
        default:
            throw new Error('Not a valid adaptive card type.');
        }
        await turnContext.sendActivity(MessageFactory.attachment(card));
    }
}

module.exports.CommandHandler = CommandHandler;
