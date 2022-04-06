// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Import required bot configuration.
require('dotenv').config();

const http = require('http');
const https = require('https');
const restify = require('restify');

// Import required bot services.
// See https://aka.ms/bot-services to learn more about the different parts of a bot.
const {
  ActivityTypes,
  ChannelServiceRoutes,
  CloudAdapter,
  CloudSkillHandler,
  ConversationState,
  ConfigurationServiceClientCredentialFactory,
  createBotFrameworkAuthenticationFromConfiguration,
  InputHints,
  MemoryStorage,
  MessageFactory,
  SkillConversationIdFactory
} = require('botbuilder');
const {
  allowedCallersClaimsValidator,
  AuthenticationConfiguration,
  AuthenticationConstants
} = require('botframework-connector');

const { SkillBot } = require('./bots/skillBot');
const { ActivityRouterDialog } = require('./dialogs/activityRouterDialog');
const { SsoSaveStateMiddleware } = require('./middleware/ssoSaveStateMiddleware');
const { DefaultConfig } = require('./config');

const config = new DefaultConfig();

// Create HTTP server
const server = restify.createServer({ maxParamLength: 1000 });
server.use(restify.plugins.acceptParser(server.acceptable));
server.use(restify.plugins.queryParser());
server.use(restify.plugins.bodyParser());

server.listen(config.Port, () => {
  console.log(`\n${server.name} listening to ${server.url}`);
  console.log('\nGet Bot Framework Emulator: https://aka.ms/botframework-emulator');
  console.log('\nTo talk to your bot, open the emulator select "Open Bot"');
});

const maxTotalSockets = (preallocatedSnatPorts, procCount = 1, weight = 0.5, overcommit = 1.1) =>
  Math.min(
    Math.floor((preallocatedSnatPorts / procCount) * weight * overcommit),
    preallocatedSnatPorts
  );

const allowedCallers = (process.env.AllowedCallers || '').split(',').filter((val) => val) || [];

const claimsValidators = allowedCallersClaimsValidator(allowedCallers);

// If the MicrosoftAppTenantId is specified in the environment config, add the tenant as a valid JWT token issuer for Bot to Skill conversation.
// The token issuer for MSI and single tenant scenarios will be the tenant where the bot is registered.
let validTokenIssuers = [];
const { MicrosoftAppTenantId } = process.env;

if (MicrosoftAppTenantId) {
  // For SingleTenant/MSI auth, the JWT tokens will be issued from the bot's home tenant.
  // Therefore, these issuers need to be added to the list of valid token issuers for authenticating activity requests.
  validTokenIssuers = [
    `${AuthenticationConstants.ValidTokenIssuerUrlTemplateV1}${MicrosoftAppTenantId}/`,
    `${AuthenticationConstants.ValidTokenIssuerUrlTemplateV2}${MicrosoftAppTenantId}/v2.0/`,
    `${AuthenticationConstants.ValidGovernmentTokenIssuerUrlTemplateV1}${MicrosoftAppTenantId}/`,
    `${AuthenticationConstants.ValidGovernmentTokenIssuerUrlTemplateV2}${MicrosoftAppTenantId}/v2.0/`
  ];
}

// Define our authentication configuration.
const authConfig = new AuthenticationConfiguration([], claimsValidators, validTokenIssuers);

const credentialsFactory = new ConfigurationServiceClientCredentialFactory({
  MicrosoftAppId: process.env.MicrosoftAppId,
  MicrosoftAppPassword: process.env.MicrosoftAppPassword,
  MicrosoftAppType: process.env.MicrosoftAppType,
  MicrosoftAppTenantId: process.env.MicrosoftAppTenantId
});

const botFrameworkAuthentication = createBotFrameworkAuthenticationFromConfiguration(
  null,
  credentialsFactory,
  authConfig,
  undefined,
  {
    agentSettings: {
      http: new http.Agent({
        keepAlive: true,
        maxTotalSockets: maxTotalSockets(1024, 4, 0.3)
      }),
      https: new https.Agent({
        keepAlive: true,
        maxTotalSockets: maxTotalSockets(1024, 4, 0.7)
      })
    }
  }
);

// Create adapter.
// See https://aka.ms/about-bot-adapter to learn more about how bots work.
const adapter = new CloudAdapter(botFrameworkAuthentication);

// Catch-all for errors.
adapter.onTurnError = async (context, error) => {
  // This check writes out errors to console log .vs. app insights.
  // NOTE: In production environment, you should consider logging this to Azure application insights.
  console.error(`\n [onTurnError] unhandled error: ${error}`);

  try {
    const { message, stack } = error;

    // Send a message to the user.
    let errorMessageText = 'The skill encountered an error or bug.';
    let errorMessage = MessageFactory.text(`${errorMessageText}\r\n${message}\r\n${stack}`, errorMessageText, InputHints.IgnoringInput);
    errorMessage.value = { message, stack };
    await context.sendActivity(errorMessage);

    errorMessageText = 'To continue to run this bot, please fix the bot source code.';
    errorMessage = MessageFactory.text(errorMessageText, errorMessageText, InputHints.ExpectingInput);
    await context.sendActivity(errorMessage);

    // Send a trace activity, which will be displayed in Bot Framework Emulator
    await context.sendTraceActivity(
      'OnTurnError Trace',
      `${error}`,
      'https://www.botframework.com/schemas/error',
      'TurnError'
    );

    // Send and EndOfConversation activity to the skill caller with the error to end the conversation
    // and let the caller decide what to do.
    await context.sendActivity({
      type: ActivityTypes.EndOfConversation,
      code: 'SkillError',
      text: error.message
    });
  } catch (err) {
    console.error(`\n [onTurnError] Exception caught in onTurnError : ${err}`);
  }
};

const continuationParametersStore = {};

// Define the state store for your bot.
// See https://aka.ms/about-bot-state to learn more about using MemoryStorage.
// A bot requires a state storage system to persist the dialog and user state between messages.
const memoryStorage = new MemoryStorage();

// Create conversation and user state with in-memory storage provider.
const conversationState = new ConversationState(memoryStorage);

adapter.use(new SsoSaveStateMiddleware(conversationState));

// Create the conversationIdFactory
const conversationIdFactory = new SkillConversationIdFactory(memoryStorage);

// Create the skill client
const skillClient = botFrameworkAuthentication.createBotFrameworkClient();

// Create the main dialog.
const dialog = new ActivityRouterDialog(config, conversationState, conversationIdFactory, skillClient, continuationParametersStore);

// Create the bot that will handle incoming messages.
const bot = new SkillBot(config, conversationState, dialog);

// Expose the manifest
server.get('/manifests/*', restify.plugins.serveStatic({ directory: './manifests', appendRequestPath: false }));

// Expose images
server.get('/images/*', restify.plugins.serveStatic({ directory: './images', appendRequestPath: false }));

// Listen for incoming requests.
server.post('/api/messages', async (req, res) => {
  await adapter.process(req, res, async (context) => {
    // Route to main dialog.
    await bot.run(context);
  });
});

// Create and initialize the skill classes
const handler = new CloudSkillHandler(adapter, (context) => bot.run(context), conversationIdFactory, botFrameworkAuthentication);
const skillEndpoint = new ChannelServiceRoutes(handler);
skillEndpoint.register(server, '/api/skills');

// Listen for incoming requests.
server.get('/api/music', restify.plugins.serveStatic({ directory: 'dialogs/cards/files', file: 'music.mp3' }));

// Listen for incoming notifications and send proactive messages to users.
server.get('/api/notify', async (req, res) => {
  let error;
  const { user } = req.query;

  const continuationParameters = continuationParametersStore[user];

  if (!continuationParameters) {
    res.setHeader('Content-Type', 'text/html');
    res.writeHead(200);
    res.write(`<html><body><h1>No messages sent</h1> <br/>There are no conversations registered to receive proactive messages for ${user}.</body></html>`);
    res.end();
    return;
  }

  try {
    await adapter.continueConversationAsync(config.MicrosoftAppId, continuationParameters.conversationReference, continuationParameters.oAuthScope, async context => {
      await context.sendActivity(`Got proactive message for user: ${user}`);
      await bot.run(context);
    });
  } catch (err) {
    error = err;
  }

  res.setHeader('Content-Type', 'text/html');
  res.writeHead(200);
  res.write(`<html><body><h1>Proactive messages have been sent</h1> <br/> Timestamp: ${new Date().toISOString()} <br /> Exception: ${error || ''}</body></html>`);
  res.end();
});
