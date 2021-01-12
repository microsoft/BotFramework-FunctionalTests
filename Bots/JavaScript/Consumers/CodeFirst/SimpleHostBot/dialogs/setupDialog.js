// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { InputHints, MessageFactory, DeliveryModes } = require('botbuilder');
const { ChoicePrompt, ComponentDialog, DialogSet, DialogTurnStatus, WaterfallDialog } = require('botbuilder-dialogs');
const { HostBot } = require('../bots/hostBot');

const SETUP_DIALOG = 'SetupDialog';
const CHOICE_PROMPT = 'ChoicePrompt';
const WATERFALL_STEPS = 'WaterfallSteps';

/**
 * The setup dialog for this bot.
 */
class SetupDialog extends ComponentDialog {
    constructor(conversationState, skillsConfig) {
        super(SETUP_DIALOG);

        this.deliveryModeProperty = conversationState.createProperty(HostBot.DeliveryModePropertyName);
        this.activeSkillProperty = conversationState.createProperty(HostBot.ActiveSkillPropertyName);
        this.skillsConfig = skillsConfig;

        // Define the setup dialog and its related components.
        // Add ChoicePrompt to render available skills.
        this.addDialog(new ChoicePrompt(CHOICE_PROMPT))
            // Add main waterfall dialog for this bot.
            .addDialog(new WaterfallDialog(WATERFALL_STEPS, [
                this.selectDeliveryModeStep.bind(this),
                this.selectSkillStep.bind(this),
                this.finalStep.bind(this)
            ]));

        this.initialDialogId = WATERFALL_STEPS;
    }

    /**
     * The run method handles the incoming activity (in the form of a TurnContext) and passes it through the dialog system.
     * If no dialog is active, it will start the default dialog.
     * @param {*} turnContext
     * @param {*} accessor
     */
    async run(turnContext, accessor) {
        const dialogSet = new DialogSet(accessor);
        dialogSet.add(this);

        const dialogContext = await dialogSet.createContext(turnContext);
        const results = await dialogContext.continueDialog();
        if (results.status === DialogTurnStatus.empty) {
            await dialogContext.beginDialog(this.id);
        }
    }

    /**
     * Render a prompt to select the delivery mode to use.
     */
    async selectDeliveryModeStep(stepContext) {
        // Create the PromptOptions with the delivery modes supported.
        const messageText = 'What delivery mode would you like to use?';
        const repromptMessageText = 'That was not a valid choice, please select a valid delivery mode.';
        const options = {
            prompt: MessageFactory.text(messageText, messageText, InputHints.ExpectingInput),
            retryPrompt: MessageFactory.text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
            choices: [DeliveryModes.Normal, DeliveryModes.ExpectReplies]
        };

        // Prompt the user to select a delivery mode.
        return await stepContext.prompt(CHOICE_PROMPT, options);
    }

    /**
     * Render a prompt to select the skill to call.
     */
    async selectSkillStep(stepContext) {
        // Set delivery mode.
        await this.deliveryModeProperty.set(stepContext.context, stepContext.result.value);

        // Create the PromptOptions from the skill configuration which contains the list of configured skills.
        const messageText = 'What skill would you like to call?';
        const repromptMessageText = 'That was not a valid choice, please select a valid skill.';
        const options = {
            prompt: MessageFactory.text(messageText, messageText, InputHints.ExpectingInput),
            retryPrompt: MessageFactory.text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
            choices: Object.keys(this.skillsConfig.skills)
        };

        // Prompt the user to select a skill.
        return await stepContext.prompt(CHOICE_PROMPT, options);
    }

    /**
     * The SetupDialog has ended, we go back to the HostBot to connect with the selected skill.
     */
    async finalStep(stepContext) {
        const selectedSkill = this.skillsConfig.skills[stepContext.result.value];

        // Set active skill
        await this.activeSkillProperty.set(stepContext.context, selectedSkill);

        const message = MessageFactory.text('Type anything to send to the skill.');
        await stepContext.context.sendActivity(message);

        return await stepContext.endDialog();
    }
}

module.exports.SetupDialog = SetupDialog;
