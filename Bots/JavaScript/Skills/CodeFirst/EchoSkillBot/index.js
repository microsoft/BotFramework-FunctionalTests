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
  CallerIdConstants,
  CloudAdapter,
  InputHints,
  MessageFactory
} = require('botbuilder');

const {
  AuthenticationConfiguration,
  AuthenticationConstants,
  BotFrameworkAuthenticationFactory,
  PasswordServiceClientCredentialFactory,
  allowedCallersClaimsValidator
} = require('botframework-connector');

// This bot's main dialog.
const { EchoBot } = require('./bot.js');

// Create HTTP server
const server = restify.createServer({ maxParamLength: 1000 });
server.use(restify.plugins.acceptParser(server.acceptable));
server.use(restify.plugins.queryParser());
server.use(restify.plugins.bodyParser());

server.listen(process.env.port || process.env.PORT || 36400, () => {
  console.log(`\n${server.name} listening to ${server.url}`);
  console.log(
    '\nGet Bot Framework Emulator: https://aka.ms/botframework-emulator'
  );
  console.log('\nTo talk to your bot, open the emulator select "Open Bot"');
});

// Expose the manifest
server.get(
  '/manifests/*',
  restify.plugins.serveStatic({
    directory: './manifests',
    appendRequestPath: false
  })
);

const maxTotalSockets = (
  preallocatedSnatPorts,
  procCount = 1,
  weight = 0.5,
  overcommit = 1.1
) =>
  Math.min(
    Math.floor((preallocatedSnatPorts / procCount) * weight * overcommit),
    preallocatedSnatPorts
  );

const allowedCallers =
  (process.env.AllowedCallers || '').split(',').filter((val) => val) || [];

const authConfig = new AuthenticationConfiguration(
  [],
  allowedCallersClaimsValidator(allowedCallers)
);

const botFrameworkAuthentication = BotFrameworkAuthenticationFactory.create(
  '',
  true,
  AuthenticationConstants.ToChannelFromBotLoginUrl,
  AuthenticationConstants.ToChannelFromBotOAuthScope,
  AuthenticationConstants.ToBotFromChannelTokenIssuer,
  AuthenticationConstants.OAuthUrl,
  AuthenticationConstants.ToBotFromChannelOpenIdMetadataUrl,
  AuthenticationConstants.ToBotFromEmulatorOpenIdMetadataUrl,
  CallerIdConstants.PublicAzureChannel,
  new PasswordServiceClientCredentialFactory(
    process.env.MicrosoftAppId || '',
    process.env.MicrosoftAppPassword || ''
  ),
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
  // NOTE: In production environment, you should consider logging this to Azure
  //       application insights.
  console.error(`\n [onTurnError] unhandled error: ${error}`);

  try {
    const { message, stack } = error;

    // Send a message to the user.
    let errorMessageText = 'The skill encountered an error or bug.';
    let errorMessage = MessageFactory.text(
      `${errorMessageText}\r\n${message}\r\n${stack}`,
      errorMessageText,
      InputHints.IgnoringInput
    );
    errorMessage.value = { message, stack };
    await context.sendActivity(errorMessage);

    errorMessageText =
      'To continue to run this bot, please fix the bot source code.';
    errorMessage = MessageFactory.text(
      errorMessageText,
      errorMessageText,
      InputHints.ExpectingInput
    );
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

// Create the bot that will handle incoming messages.
const myBot = new EchoBot();

// Listen for incoming requests.
server.post('/api/messages', async (req, res) => {
  await adapter.process(req, res, async (context) => {
    // Route to main dialog.
    await myBot.run(context);
  });
});
