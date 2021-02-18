// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ActivityTypes, DeliveryModes, InputHints, MessageFactory,} = require('botbuilder');
const { ChoicePrompt, ChoiceFactory, ComponentDialog, DialogSet, ListStyle, SkillDialog, WaterfallDialog } = require('botbuilder-dialogs');
const { RootBot } = require('../bots/rootBot');
const { SsoDialog } = require('./sso/ssoDialog');
const { TangentDialog } = require('./tangentDialog');

const MAIN_DIALOG = 'MainDialog';
const DELIVERY_PROMPT = 'DeliveryModePrompt';
const SKILL_TYPE_PROMPT = 'SkillTypePrompt';
const SKILL_PROMPT = 'SkillPrompt';
const SKILL_ACTION_PROMPT = 'SkillActionPrompt';
const SSO_DIALOG = 'SsoDialog';
const TANGENT_DIALOG = 'TangentDialog';
const WATERFALL_DIALOG = 'WaterfallDialog';
// Constants used for selecting actions on the skill.
const JUST_FORWARD_THE_ACTIVITY = "JustForwardTurnContext.Activity";

class MainDialog extends ComponentDialog {
    constructor(conversationState, skillsConfig, skillClient, conversationIdFactory) {
        super(MAIN_DIALOG);

        const botId = process.env.MicrosoftAppId;

        if (!conversationState) throw new Error('[MainDialog]: Missing parameter \'conversationState\' is required');
        if (!skillsConfig) throw new Error('[MainDialog]: Missing parameter \'skillsConfig\' is required');
        if (!skillClient) throw new Error('[MainDialog]: Missing parameter \'skillClient\' is required');
        if (!conversationIdFactory) throw new Error('[MainDialog]: Missing parameter \'conversationIdFactory\' is required');

        this.deliveryModeProperty = conversationState.createProperty(RootBot.DeliveryModePropertyName);
        this.activeSkillProperty = conversationState.createProperty(RootBot.ActiveSkillPropertyName);
        this.skillsConfig = skillsConfig;
        this.deliveryMode = '';
        this.selectedSkill = '';

        // Register the tangent dialog for testing tangents and resume.
        this.addDialog(new TangentDialog(TANGENT_DIALOG));

        // Create and add SkillDialog instances for the configured skills.
        this.addSkillDialogs(conversationState, conversationIdFactory, skillClient, skillsConfig, botId);

        // Add ChoicePrompt to render available delivery modes.
        this.addDialog(new ChoicePrompt(DELIVERY_PROMPT));

        // Add ChoicePrompt to render available types of skill.
        this.addDialog(new ChoicePrompt(SKILL_TYPE_PROMPT));

        // Add ChoicePrompt to render available skills.
        this.addDialog(new ChoicePrompt(SKILL_PROMPT));

        // Add ChoicePrompt to render skill actions.
        this.addDialog(new ChoicePrompt(SKILL_ACTION_PROMPT, this.skillActionPromptValidator));

        // Add dialog to prepare SSO on the host and test the SSO skill
        // The waterfall skillDialog created in AddSkillDialogs contains the SSO skill action.
        let waterfallDialog = Object.values(this.dialogs.dialogs).find(e => e.id.startsWith('WaterfallSkill'));
        this.addDialog(new SsoDialog(waterfallDialog));

        this.addDialog(new WaterfallDialog(WATERFALL_DIALOG, [
            this.selectDeliveryModeStep.bind(this),
            this.selectSkillTypeStep.bind(this),
            this.selectSkillStep.bind(this),
            this.selectSkillActionStep.bind(this),
            this.callSkillActionStep.bind(this),
            this.finalStep.bind(this)
        ]));

        this.initialDialogId = WATERFALL_DIALOG;
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

    async onContinueDialog(innerDc) {
        const activeSkill = await this.activeSkillProperty.get(innerDc.context, () => null);
        const activity = innerDc.context.activity;
        if (activeSkill != null && activity.type === ActivityTypes.Message && activity.text != null && activity.text.toLowerCase() === 'abort') {
            // Cancel all dialogs when the user says abort.
            // The SkillDialog automatically sends an EndOfConversation message to the skill to let the
            // skill know that it needs to end its current dialogs, too.
            await innerDc.cancelAllDialogs();
            return await innerDc.replaceDialog(this.initialDialogId, { text: 'Canceled! \n\n What delivery mode would you like to use?' });
        }
        // Sample to test a tangent when in the middle of a skill conversation.
        if (activeSkill != null && activity.type === ActivityTypes.Message && activity.text != null && activity.text.toLowerCase() === 'tangent') {
            // Start tangent.
            return await innerDc.beginDialog(TANGENT_DIALOG);
        }

        return await super.onContinueDialog(innerDc);
    }

    /**
     * Render a prompt to select the delivery mode to use.
     */
    async selectDeliveryModeStep(stepContext) {
        // Create the PromptOptions with the delivery modes supported.
        const messageText = stepContext.options && stepContext.options.text ? stepContext.options.text : 'What delivery mode would you like to use?';
        const repromptMessageText = 'That was not a valid choice, please select a valid delivery mode.';

        return await stepContext.prompt(DELIVERY_PROMPT, {
            prompt: MessageFactory.text(messageText, messageText, InputHints.ExpectingInput),
            retryPrompt: MessageFactory.text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
            choices: ChoiceFactory.toChoices([DeliveryModes.Normal, DeliveryModes.ExpectReplies])
        });
    }

    /**
     * Render a prompt to select the type of skill to use.
     */
    async selectSkillTypeStep(stepContext) {
        // Set delivery mode.
        this.deliveryMode = stepContext.result.value;
        await this.deliveryModeProperty.set(stepContext.context, stepContext.result.value);

        const messageText = "What type of skill would you like to use?";
        const repromptMessageText = "That was not a valid choice, please select a valid skill type.";

        // Create the PromptOptions from the skill configuration which contains the list of configured skills.
        return await stepContext.prompt(SKILL_TYPE_PROMPT, {
            prompt: MessageFactory.text(messageText, messageText, InputHints.ExpectingInput),
            retryPrompt: MessageFactory.text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
            choices: ChoiceFactory.toChoices(['EchoSkill', 'WaterfallSkill']),
            style: ListStyle.suggestedAction
        });
    }

    /**
     * Render a prompt to select the skill to call.
     */
    async selectSkillStep(stepContext) {
        let skillType = stepContext.result.value;

        // Create the PromptOptions from the skill configuration which contains the list of configured skills.
        const messageText = 'What skill would you like to call?';
        const repromptMessageText = 'That was not a valid choice, please select a valid skill.';

        return await stepContext.prompt(SKILL_PROMPT, {
            prompt: MessageFactory.text(messageText, messageText, InputHints.ExpectingInput),
            retryPrompt: MessageFactory.text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
            choices: ChoiceFactory.toChoices((Object.keys(this.skillsConfig.skills)).filter(skill => skill.startsWith(skillType))),
            style: ListStyle.suggestedAction
        });
    }

    /**
     * Render a prompt to select the begin action for the skill.
     */
    async selectSkillActionStep(stepContext) {
        this.selectedSkill = this.skillsConfig.skills[stepContext.result.value];
        let v3Bots = [ 'EchoSkillBotDotNetV3', 'EchoSkillBotJSV3' ];

        // Set active skill.
        await this.activeSkillProperty.set(stepContext.context, this.selectedSkill);

        // Exclude v3 bots from ExpectReplies.
        if (this.deliveryMode === DeliveryModes.ExpectReplies && v3Bots.includes(this.selectedSkill.definition.id))
        {
            await stepContext.context.SendActivityAsync(MessageFactory.text("V3 Bots do not support 'expectReplies' delivery mode."));

            // Forget delivery mode and skill invocation.
            await this.deliveryModeProperty.delete(stepContext.context);
            await this.activeSkillProperty.delete(stepContext.context);
            
            // Restart setup dialog.
            return await stepContext.replaceDialog(this.initialDialogId);
        }

        // Create the PromptOptions with the actions supported by the selected skill.
        const messageText = `Select an action # to send to **${ this.selectedSkill.definition.id }**.\n\nOr just type in a message and it will be forwarded to the skill as a message activity.`;

        return await stepContext.prompt(SKILL_ACTION_PROMPT, {
            prompt: MessageFactory.text(messageText, messageText, InputHints.ExpectingInput),
            choices: this.selectedSkill.definition.getActions(),
        });
    }

    async skillActionPromptValidator(promptContext) {
        if (!promptContext.recognized.succeeded) {
            // Assume the user wants to send a message if an item in the list is not selected.
            promptContext.recognized.value = new FoundChoice(value = JustForwardTheActivity);
        }
        return true;
    }

    async callSkillActionStep(stepContext) {

        let skillActivity = this.createBeginActivity(stepContext.context, this.selectedSkill.definition, stepContext.result.value);

        // Create the BeginSkillDialogOptions and assign the activity to send.
        let skillDialogArgs = { activity: skillActivity };

        if (this.deliveryMode === DeliveryModes.ExpectReplies) {
            skillDialogArgs.activity.deliveryMode = DeliveryModes.ExpectReplies;
        }

        if (skillActivity.name == "Sso") {
            // Special case, we start the SSO dialog to prepare the host to call the skill.
            return await stepContext.beginDialog(SSO_DIALOG);
        }

        // Start the skillDialog instance with the arguments.
        return await stepContext.beginDialog(this.selectedSkill.definition.id, skillDialogArgs);
    }

    /**
     * The SkillDialog has ended, render the results (if any) and restart MainDialog.
     */
    async finalStep(stepContext) {
        if (stepContext.result) {
            let message = `Skill \"${ this.selectedSkill.definition.id }\" invocation complete.`;
            message += ` Result: ${ JSON.SerializeObject(stepContext.result) }`;
            await stepContext.context.sendActivity(message);
        }

        // Forget delivery mode and skill invocation.
        await this.deliveryModeProperty.delete(stepContext.context);
        await this.activeSkillProperty.delete(stepContext.context);

        // Restart setup dialog
        return await stepContext.replaceDialog(this.initialDialogId, { text: `Done with \"${ this.selectedSkill.definition.id }\". \n\n What delivery mode would you like to use?` });
    }

    // Helper method that creates and adds SkillDialog instances for the configured skills.
    addSkillDialogs(conversationState, conversationIdFactory, skillClient, skillsConfig, botId) {
        Object.keys(skillsConfig.skills).forEach((skillId) => {
            const skillInfo = skillsConfig.skills[skillId];
            
            const skillDialogOptions = {
                botId: botId,
                conversationIdFactory,
                conversationState,
                skill: skillInfo,
                skillHostEndpoint: process.env.SkillHostEndpoint,
                skillClient
            }

            // Add a SkillDialog for the selected skill.
            this.addDialog(new SkillDialog(skillDialogOptions, skillInfo.definition.id));
        });
    }

    // Helper method to create the activity to be sent to the DialogSkillBot using selected type and values.
    createBeginActivity(turnContext, skillId, selectedOption) {
        if (selectedOption === JUST_FORWARD_THE_ACTIVITY) {
            // Note message activities also support input parameters but we are not using them in this example.
            // Return a clone of the activity so we don't risk altering the original one.
            return Object.assign({}, turnContext.activity);
        }
        // Get the begin activity from the skill instance.
        let activity = skillId.createBeginActivity(selectedOption);

        // We are manually creating the activity to send to the skill; ensure we add the ChannelData and Properties
        // from the original activity so the skill gets them.
        // Note: this is not necessary if we are just forwarding the current activity from context.
        activity.channelData = turnContext.activity.channelData;
        activity.properties = turnContext.activity.properties;
        return activity;
    }
}

module.exports.MainDialog = MainDialog;
