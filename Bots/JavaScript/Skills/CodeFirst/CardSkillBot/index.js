// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const dotenv = require('dotenv');
const path = require('path');
const restify = require('restify');

// Import required bot services.
// See https://aka.ms/bot-services to learn more about the different parts of a bot.
const { ActivityTypes, BotFrameworkAdapter, InputHints } = require('botbuilder');
const { AuthenticationConfiguration } = require('botframework-connector');

// Import required bot configuration.
const ENV_FILE = path.join(__dirname, '.env');
dotenv.config({ path: ENV_FILE });

// This bot's main dialog.
const { EchoBot } = require('./bot');
const { allowedCallersClaimsValidator } = require('./authentication/allowedCallersClaimsValidator');

// Create HTTP server
const server = restify.createServer();
server.listen(process.env.port || process.env.PORT || 39783, () => {
    console.log(`\n${ server.name } listening to ${ server.url }`);
    console.log('\nGet Bot Framework Emulator: https://aka.ms/botframework-emulator');
    console.log('\nTo talk to your bot, open the emulator select "Open Bot"');
});

// Expose the manifest
server.get('/manifest/*', restify.plugins.serveStatic({ directory: './manifest', appendRequestPath: false }));

// Create adapter.
// See https://aka.ms/about-bot-adapter to learn more about how bots work.
const adapter = new BotFrameworkAdapter({
    appId: process.env.MicrosoftAppId,
    appPassword: process.env.MicrosoftAppPassword,
    authConfig: new AuthenticationConfiguration([], allowedCallersClaimsValidator)
});

// Catch-all for errors.
adapter.onTurnError = async (context, error) => {
    // This check writes out errors to the console log, instead of to app insights.
    // NOTE: In a production environment, you should consider logging this to Azure
    //       application insights.
    console.error(`\n [onTurnError] unhandled error: ${ error }`);

    await sendErrorMessage(context, error);
    await sendEoCToParent(context, error);
};

async function sendErrorMessage(context, error) {
    try {
        // Send a message to the user.
        let onTurnErrorMessage = 'The skill encountered an error or bug.';
        await context.sendActivity(onTurnErrorMessage, onTurnErrorMessage, InputHints.ExpectingInput);

        onTurnErrorMessage = 'To continue to run this bot, please fix the bot source code.';
        await context.sendActivity(onTurnErrorMessage, onTurnErrorMessage, InputHints.ExpectingInput);

        // Send a trace activity, which will be displayed in the Bot Framework Emulator.
        // Note: we return the entire exception in the value property to help the developer;
        // this should not be done in production.
        await context.sendTraceActivity('OnTurnError Trace', error.toString(), 'https://www.botframework.com/schemas/error', 'TurnError');
    } catch (err) {
        console.error(`\n [onTurnError] Exception caught in sendErrorMessage: ${ err }`);
    }
}

async function sendEoCToParent(context, error) {
    try {
        // Send an EndOfConversation activity to the skill caller with the error to end the conversation,
        // and let the caller decide what to do.
        const endOfConversation = {
            type: ActivityTypes.EndOfConversation,
            code: 'SkillError',
            text: error.toString()
        };
        await context.sendActivity(endOfConversation);
    } catch (err) {
        console.error(`\n [onTurnError] Exception caught in sendEoCToParent: ${ err }`);
    }
}

// Create the main dialog.
const myBot = new EchoBot();

// Listen for incoming requests.
server.post('/api/messages', (req, res) => {
    adapter.processActivity(req, res, async (context) => {
        // Route to main dialog.
        await myBot.run(context);
    });
});
