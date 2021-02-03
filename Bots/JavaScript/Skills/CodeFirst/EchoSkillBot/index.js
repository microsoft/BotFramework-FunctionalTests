// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const dotenv = require('dotenv');
const path = require('path');
const restify = require('restify');

// Import required bot services.
// See https://aka.ms/bot-services to learn more about the different parts of a bot.
const { ActivityTypes, BotFrameworkAdapter, InputHints, MemoryStorage, ConversationState, UserState } = require('botbuilder');
const { AuthenticationConfiguration } = require('botframework-connector');

// Import required bot configuration.
const ENV_FILE = path.join(__dirname, '.env');
dotenv.config({ path: ENV_FILE });

// This bot's main dialog.
const { EchoBot } = require('./bot');
const { MainDialog } = require('./dialogs/mainDialog');
const { allowedCallersClaimsValidator } = require('./authentication/allowedCallersClaimsValidator');

// Create HTTP server
const server = restify.createServer();
server.listen(process.env.port || process.env.PORT || 36400, () => {
    console.log(`\n${ server.name } listening to ${ server.url }`);
    console.log('\nGet Bot Framework Emulator: https://aka.ms/botframework-emulator');
    console.log('\nTo talk to your bot, open the emulator select "Open Bot"');
});

// Expose the manifest
server.get('/manifests/*', restify.plugins.serveStatic({ directory: './manifests', appendRequestPath: false }));

// Create adapter.
// See https://aka.ms/about-bot-adapter to learn more about how bots work.
const adapter = new BotFrameworkAdapter({
    appId: process.env.MicrosoftAppId,
    appPassword: process.env.MicrosoftAppPassword,
    authConfig: new AuthenticationConfiguration([], allowedCallersClaimsValidator)
});

// Catch-all for errors.
adapter.onTurnError = async (context, error) => {
    // This check writes out errors to console log .vs. app insights.
    // NOTE: In production environment, you should consider logging this to Azure
    //       application insights.
    console.error(`\n [onTurnError] unhandled error: ${ error }`);

    // Send a trace activity, which will be displayed in Bot Framework Emulator
    await context.sendTraceActivity(
        'OnTurnError Trace',
        `${ error }`,
        'https://www.botframework.com/schemas/error',
        'TurnError'
    );

    // Send a message to the user
    let onTurnErrorMessage = 'The skill encountered an error or bug.';
    await context.sendActivity(onTurnErrorMessage, onTurnErrorMessage, InputHints.IgnoringInput);
    onTurnErrorMessage = 'To continue to run this bot, please fix the bot source code.';
    await context.sendActivity(onTurnErrorMessage, onTurnErrorMessage, InputHints.ExpectingInput);

    // Send and EndOfConversation activity to the skill caller with the error to end the conversation
    // and let the caller decide what to do.
    const endOfConversation = {
        type: ActivityTypes.EndOfConversation,
        code: 'SkillError',
        text: error
    };
    await context.sendActivity(endOfConversation);
};

// Define the state store for your bot.
// See https://aka.ms/about-bot-state to learn more about using MemoryStorage.
// A bot requires a state storage system to persist the dialog and user state between messages.
const memoryStorage = new MemoryStorage();

// Create conversation and user state with in-memory storage provider.
const conversationState = new ConversationState(memoryStorage);
const userState = new UserState(memoryStorage);

// Create the main dialog.
const dialog = new MainDialog();

// Create the bot that will handle incoming messages.
const myBot = new EchoBot(conversationState, userState, dialog);

// Listen for incoming requests.
server.post('/api/messages', (req, res) => {
    adapter.processActivity(req, res, async (context) => {
        // Route to main dialog.
        await myBot.run(context);
    });
});
