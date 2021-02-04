// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const restify = require('restify');
const builder = require('botbuilder');
require('dotenv').config();

// Setup Restify Server
const server = restify.createServer();
server.listen(process.env.port || process.env.PORT || 36407, function () {
   console.log('%s listening to %s', server.name, server.url); 
});

// Expose the manifest
server.get('/manifests/*', restify.plugins.serveStatic({ directory: './manifests', appendRequestPath: false }));
  
// Bot Storage: Here we register the state storage for your bot. 
// Default store: volatile in-memory store - Only for prototyping!
// We provide adapters for Azure Table, CosmosDb, SQL Azure, or you can implement your own!
// For samples and documentation, see: https://github.com/Microsoft/BotBuilder-Azure
const inMemoryStorage = new builder.MemoryBotStorage();

// Create chat connector for communicating with the Bot Framework Service
const connector = new builder.ChatConnector({
    appId: process.env.MicrosoftAppId,
    appPassword: process.env.MicrosoftAppPassword,
    enableSkills: true,
    allowedCallers: [process.env.allowedCallers]
});

// Listen for messages from users 
server.post('/api/messages', connector.listen());

// Create your bot with a function to receive messages from the user
const bot = new builder.UniversalBot(connector, function (session) {
    switch (session.message.text.toLowerCase()) {
        case 'end':
        case 'stop':
            session.say("Ending conversation from the skill...", {
                inputHint: builder.InputHint.acceptingInput
            });
            session.endConversation();
            break;
        default:
            session.say('Echo: ' + session.message.text, {
                inputHint: builder.InputHint.acceptingInput
            });
            session.say('Say "end" or "stop" and I\'ll end the conversation and back to the parent.');
    }
}).set('storage', inMemoryStorage); // Register in memory storage
