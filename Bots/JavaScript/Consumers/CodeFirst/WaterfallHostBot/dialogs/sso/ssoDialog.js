// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ActivityTypes, BeginSkillDialogOptions, InputHints, MessageFactory } = require('botbuilder');
const { ComponentDialog, Choice, ChoicePrompt, DialogTurnResult, DialogTurnStatus, WaterfallDialog } = require('botbuilder-dialogs');

const SSO_DIALOG = 'SsoDialog';
const ACTION_STEP_PROMPT = 'ActionStepPrompt';
const WATERFALL_DIALOG = 'WaterfallDialog';

// Helps prepare the host for SSO operations and provides helpers to check the status and invoke the skill.
class SsoDialog extends ComponentDialog {
    constructor(skillDialog) {
        super(SSO_DIALOG);

        this.connectionName = process.env.SsoConnectionName;
        this.skillDialogId = skillDialog.id;

        this.addDialog(new ChoicePrompt(ACTION_STEP_PROMPT));
        this.addDialog(new SsoSignInDialog(this.connectionName));
        this.addDialog(skillDialog);

        this.addDialog(new WaterfallDialog(WATERFALL_DIALOG, [
            this.promptActionStep.bind(this),
            this.handleActionStep.bind(this),
            this.promptFinalStep.bind(this)
        ]));

        this.initialDialogId = WATERFALL_DIALOG;
    }

    async promptActionStep(stepContext) {
        const messageText = 'What SSO action do you want to perform?';
        const repromptMessageText = 'That was not a valid choice, please select a valid choice.';

        return await stepContext.prompt(DELIVERY_PROMPT, {
            prompt: MessageFactory.text(messageText, messageText, InputHints.ExpectingInput),
            retryPrompt: MessageFactory.text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
            choices: await this.getPromptChoices(stepContext)
        });
    }

    async getPromptChoices(stepContext) {
        let promptChoices = [];
        let adapter = stepContext.context.adapter;
        let token = await adapter.getUserToken(stepContext.context, this.connectionName);

        if (!token) {
            promptChoices.push = new Choice('Login');
            // Token exchange will fail when the host is not logged on and the skill should 
            // show a regular OAuthPrompt.
            promptChoices.push = new Choice('Call Skill (without SSO)');
        } else {
            promptChoices.push = new Choice('Logout');
            promptChoices.push = new Choice('Show token');
            promptChoices.push = new Choice('Call Skill (with SSO)');
        }

        promptChoices.push = new Choice('Back');

        return promptChoices;
    }

    async handleActionStep(stepContext) {
        const action = stepContext.result.value;

        switch (action) {
            case 'login':
                return await stepContext.beginDialog(SSO_DIALOG);
            case 'logout':
                const adapter = stepContext.context.adapter;
                await adapter.signOutUser(stepContext.context, this.connectionName);
                await stepContext.context.sendActivity('You have been signed out.');
                return await stepContext.next();
            case 'show token':
                const tokenProvider = stepContext.context.adapter;
                const token = await tokenProvider.getUserToken(stepContext.context, this.connectionName);
                if (!token) {
                    await stepContext.context.sendActivity('User has no cached token.');
                } else {
                    await stepContext.context.sendActivity(`Here is your current SSO token: ${ token.token }`);
                }
                return await stepContext.next();
            case 'call skill (with sso)':
            case 'call skill (without sso)':
                let beginSkillActivity = new Activity();
                beginSkillActivity.type = ActivityTypes.Event;
                beginSkillActivity.name = 'Sso';
                return await stepContext.beginDialog(this.skillDialogId, new BeginSkillDialogOptions( Activity = beginSkillActivity));
            case 'back':
                return new DialogTurnResult({status: DialogTurnStatus.complete})
            default:
                // This should never be hit since the previous prompt validates the choice.
                throw new Error(`[SsoDialog]: Unrecognized action: ${ action }`);
        }
    }

    async promptFinalStep(stepContext) {
        // Restart the dialog (we will exit when the user says end).
        return await stepContext.replaceDialog(this.initialDialogId);
    }
}

module.exports.SsoDialog = SsoDialog;
