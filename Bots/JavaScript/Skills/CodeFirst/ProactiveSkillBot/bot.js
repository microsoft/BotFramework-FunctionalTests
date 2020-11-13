// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ActivityHandler, ActivityTypes, EndOfConversationCodes, TurnContext } = require('botbuilder');

class EchoBot extends ActivityHandler {
    constructor(conversationReferences) {
        super();

        this.conversationReferences = conversationReferences;

        this.onConversationUpdate(async (context, next) => {
            this.addConversationReference(context.activity);

            await next();
        });

        // See https://aka.ms/about-bot-activity-message to learn more about the message and other activity types.
        this.onMessage(async (context, next) => {
            switch (context.activity.text.toLowerCase()) {
            case 'end':
            case 'stop':
                await context.sendActivity({
                    type: ActivityTypes.EndOfConversation,
                    code: EndOfConversationCodes.CompletedSuccessfully
                });
                break;
            default:
                this.addConversationReference(context.activity);

                // Echo back what the user said
                await context.sendActivity(`You sent '${ context.activity.text }'`);
                await context.sendActivity('Navigate to http://localhost:39783/api/notify to proactively message everyone who has previously messaged this bot.');
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
    
    addConversationReference(activity) {
        const conversationReference = TurnContext.getConversationReference(activity);
        this.conversationReferences[conversationReference.conversation.id] = conversationReference;
    };
}

module.exports.EchoBot = EchoBot;
