// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ActivityHandler, ActivityTypes, MessageFactory, EndOfConversationCodes } = require('botbuilder');
const { CommandHandler } = require('./commandHandler');
class EchoBot extends ActivityHandler {
  constructor () {
    super();
    // See https://aka.ms/about-bot-activity-message to learn more about the message and other activity types.
    this.onMessage(async (context, next) => {
      if (context.activity.text) {
        switch (context.activity.text.toLowerCase()) {
          case 'end':
          case 'stop':
            await context.sendActivity({
              type: ActivityTypes.EndOfConversation,
              code: EndOfConversationCodes.CompletedSuccessfully
            });
            break;
          default:
            await CommandHandler.handleCommand(context, context.activity.text, this);
        }
      } else if (context.activity.value) {
        // This was a message from the card.
        const obj = context.activity.value;

        await context.sendActivity(MessageFactory.text(`This was the value of the activity ${JSON.stringify(obj)}`));
      }

      // By calling next() you ensure that the next BotHandler is run.
      await next();
    });

    this.onEndOfConversation(async (context, next) => {
      // This will be called if the root bot is ending the conversation.  Sending additional messages should be
      // avoided as the conversation may have been deleted.
      // Perform cleanup of resources if needed.

      // By calling next() you ensure that the next BotHandler is run.
      await next();
    });
  }
}

module.exports.EchoBot = EchoBot;
