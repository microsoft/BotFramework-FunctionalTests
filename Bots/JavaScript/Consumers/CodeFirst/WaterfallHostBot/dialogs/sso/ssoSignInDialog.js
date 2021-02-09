// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ComponentDialog, WaterfallDialog, OAuthPrompt } = require('botbuilder-dialogs');

const SSO_SIGNIN_DIALOG = 'SsoSignInDialog';
const OAUTH_PROMPT = 'OAuthPrompt';
const WATERFALL_DIALOG = 'WaterfallDialog';

// Helps prepare the host for SSO operations and provides helpers to check the status and invoke the skill.
class SsoSignInDialog extends ComponentDialog {
    constructor(connectionName) {
        super(SSO_SIGNIN_DIALOG);

        this.addDialog(new OAuthPrompt(OAUTH_PROMPT, new OAuthPromptSettings({
            connectionName: connectionName,
            text: 'Sign in to the host bot using AAD for SSO',
            title: 'Sign In'
        })));

        this.addDialog(new WaterfallDialog(WATERFALL_DIALOG, [
            this.signInStep.bind(this),
            this.displayTokenStep.bind(this)
        ]));

        this.initialDialogId = WATERFALL_DIALOG;
    }

    async signInStep(stepContext) {
        return await stepContext.beginDialog(OAUTH_PROMPT);
    }

    async displayTokenStep(stepContext) {
        if (typeof stepContext.result !== 'TokenResponse') {
            await stepContext.context.sendActivity('No token was provided.');
        } else {
            await stepContext.context.sendActivity(`Here is your token: ${ result.Token }`);
        }
        return await stepContext.endDialog();
    }
}

module.exports.SsoSignInDialog = SsoSignInDialog;
