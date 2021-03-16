// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ActivityHandler, ActivityTypes, EndOfConversationCodes } = require('botbuilder');

class EchoBot extends ActivityHandler {
  /**
     *
     * @param {ConversationState} conversationState
     * @param {UserState} userState
     * @param {Dialog} dialog
     */
  constructor (conversationState, userState, dialog) {
    super();
    if (!conversationState) throw new Error('[DialogBot]: Missing parameter. conversationState is required');
    if (!userState) throw new Error('[DialogBot]: Missing parameter. userState is required');
    if (!dialog) throw new Error('[DialogBot]: Missing parameter. dialog is required');

    this.conversationState = conversationState;
    this.userState = userState;
    this.dialog = dialog;
    this.dialogState = this.conversationState.createProperty('DialogState');

    this.onTokenResponseEvent(async (context, next) => {
      console.log('Running dialog with Token Response Event Activity.');
      await this.dialog.run(context, this.dialogState);
      await next();
    });

    // See https://aka.ms/about-bot-activity-message to learn more about the message and other activity types.
    this.onMessage(async (context, next) => {
      switch (context.activity.text.toLowerCase()) {
        case 'auth':
        case 'yes':
        case 'no':
          // Run the Dialog with the new message Activity.
          await this.dialog.run(context, this.dialogState);
          await next();
          break;
        case 'end':
        case 'stop':
          await context.sendActivity('Ending conversation from the skill...');
          await context.sendActivity({
            type: ActivityTypes.EndOfConversation,
            code: EndOfConversationCodes.CompletedSuccessfully
          });
          break;
        default:
          await context.sendActivity(`Echo: ${context.activity.text}`);
          await context.sendActivity('Say "end" or "stop" and I\'ll end the conversation and back to the parent.');
      }

      // By calling next() you ensure that the next BotHandler is run.
      await next();
    });

    this.onUnrecognizedActivityType(async (context, next) => {
      // This will be called if the root bot is ending the conversation.  Sending additional messages should be
      // avoided as the conversation may have been deleted.
      // Perform cleanup of resources if needed.

      // By calling next() you ensure that the next BotHandler is run.
      await next();
    });
  }

  /**
     * Override the ActivityHandler.run() method to save state changes after the bot logic completes.
     */
  async run (context) {
    await super.run(context);

    // Save any state changes. The load happened during the execution of the Dialog.
    await this.conversationState.saveChanges(context, false);
    await this.userState.saveChanges(context, false);
  }
}

module.exports.EchoBot = EchoBot;
