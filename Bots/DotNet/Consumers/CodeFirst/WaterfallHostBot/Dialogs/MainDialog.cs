﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Integration.AspNet.Core.Skills;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.BotFrameworkFunctionalTests.WaterfallHostBot.Dialogs.Sso;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallHostBot.Dialogs
{
    /// <summary>
    /// The main dialog for this bot. It uses a <see cref="SkillDialog"/> to call skills.
    /// </summary>
    public class MainDialog : ComponentDialog
    {
        // State property key that stores the active skill (used in AdapterWithErrorHandler to terminate the skills on error).
        public static readonly string ActiveSkillPropertyName = $"{typeof(MainDialog).FullName}.ActiveSkillProperty";

        // Constants used for selecting actions on the skill.
        private const string JustForwardTheActivity = "JustForwardTurnContext.Activity";
        
        private readonly IStatePropertyAccessor<BotFrameworkSkill> _activeSkillProperty;
        private readonly string _deliveryMode = $"{typeof(MainDialog).FullName}.DeliveryMode";
        private readonly string _selectedSkillKey = $"{typeof(MainDialog).FullName}.SelectedSkillKey";
        private readonly SkillsConfiguration _skillsConfig;

        // Dependency injection uses this constructor to instantiate MainDialog.
        public MainDialog(ConversationState conversationState, SkillConversationIdFactoryBase conversationIdFactory, SkillHttpClient skillClient, SkillsConfiguration skillsConfig, IConfiguration configuration)
            : base(nameof(MainDialog))
        {
            var botId = configuration.GetSection(MicrosoftAppCredentials.MicrosoftAppIdKey)?.Value;

            _skillsConfig = skillsConfig ?? throw new ArgumentNullException(nameof(skillsConfig));

            if (skillClient == null)
            {
                throw new ArgumentNullException(nameof(skillClient));
            }

            if (conversationState == null)
            {
                throw new ArgumentNullException(nameof(conversationState));
            }

            // Create state property to track the active skill.
            _activeSkillProperty = conversationState.CreateProperty<BotFrameworkSkill>(ActiveSkillPropertyName);

            // Register the tangent dialog for testing tangents and resume
            AddDialog(new TangentDialog());

            // Create and add SkillDialog instances for the configured skills.
            AddSkillDialogs(conversationState, conversationIdFactory, skillClient, skillsConfig, botId);

            // Add ChoicePrompt to render available delivery modes.
            AddDialog(new ChoicePrompt("DeliveryModePrompt"));

            // Add ChoicePrompt to render available types of skill.
            AddDialog(new ChoicePrompt("SkillTypePrompt"));

            // Add ChoicePrompt to render available skills.
            AddDialog(new ChoicePrompt("SkillPrompt"));

            // Add ChoicePrompt to render skill actions.
            AddDialog(new ChoicePrompt("SkillActionPrompt", SkillActionPromptValidator));

            // Add dialog to prepare SSO on the host and test the SSO skill
            // The waterfall skillDialog created in AddSkillDialogs contains the SSO skill action.
            var waterfallDialog = Dialogs
                .GetDialogs()
                .Where(e => e.Id.StartsWith("WaterfallSkill"))
                .First();
            AddDialog(new SsoDialog(waterfallDialog, configuration));

            // Add main waterfall dialog for this bot.
            var waterfallSteps = new WaterfallStep[]
            {
                SelectDeliveryModeStepAsync,
                SelectSkillTypeStepAsync,
                SelectSkillStepAsync,
                SelectSkillActionStepAsync,
                CallSkillActionStepAsync,
                FinalStepAsync
            };
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            
            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        /// <summary>
        /// This override is used to test the "abort" command to interrupt skills from the parent and
        /// also to test the "tangent" command to start a tangent and resume a skill.
        /// </summary>
        /// <param name="innerDc">The inner <see cref="DialogContext"/> for the current turn of conversation.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            // This is an example on how to cancel a SkillDialog that is currently in progress from the parent bot.
            var activeSkill = await _activeSkillProperty.GetAsync(innerDc.Context, () => null, cancellationToken);
            var activity = innerDc.Context.Activity;
            if (activeSkill != null && activity.Type == ActivityTypes.Message && !string.IsNullOrWhiteSpace(activity.Text) && activity.Text.Equals("abort", StringComparison.CurrentCultureIgnoreCase))
            {
                // Cancel all dialogs when the user says abort.
                // The SkillDialog automatically sends an EndOfConversation message to the skill to let the
                // skill know that it needs to end its current dialogs, too.
                await innerDc.CancelAllDialogsAsync(cancellationToken);
                return await innerDc.ReplaceDialogAsync(InitialDialogId, "Canceled! \n\n What delivery mode would you like to use?", cancellationToken);
            }

            // Sample to test a tangent when in the middle of a skill conversation.
            if (activeSkill != null && activity.Type == ActivityTypes.Message && !string.IsNullOrWhiteSpace(activity.Text) && activity.Text.Equals("tangent", StringComparison.CurrentCultureIgnoreCase))
            {
                // Start tangent.
                return await innerDc.BeginDialogAsync(nameof(TangentDialog), cancellationToken: cancellationToken);
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        // Render a prompt to select the delivery mode to use.
        private async Task<DialogTurnResult> SelectDeliveryModeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Create the PromptOptions with the delivery modes supported.
            var messageText = stepContext.Options?.ToString() ?? "What delivery mode would you like to use?";
            const string rePromptMessageText = "That was not a valid choice, please select a valid delivery mode.";
            var choices = new List<Choice>
            {
                new Choice(DeliveryModes.Normal),
                new Choice(DeliveryModes.ExpectReplies)
            };
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(rePromptMessageText, rePromptMessageText, InputHints.ExpectingInput),
                Style = ListStyle.SuggestedAction,
                Choices = choices
            };

            // Prompt the user to select a delivery mode.
            return await stepContext.PromptAsync("DeliveryModePrompt", options, cancellationToken);
        }

        // Render a prompt to select the type of skill to use.
        private async Task<DialogTurnResult> SelectSkillTypeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Remember the delivery mode selected by the user.
            stepContext.Values[_deliveryMode] = ((FoundChoice)stepContext.Result).Value;

            // Create the PromptOptions with the types of supported skills.
            const string messageText = "What type of skill would you like to use?";
            const string rePromptMessageText = "That was not a valid choice, please select a valid skill type.";
            var choices = new List<Choice>
            {
                new Choice("EchoSkill"),
                new Choice("WaterfallSkill")
            };
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(rePromptMessageText, rePromptMessageText, InputHints.ExpectingInput),
                Style = ListStyle.SuggestedAction,
                Choices = choices
            };

            // Prompt the user to select a type of skill.
            return await stepContext.PromptAsync("SkillTypePrompt", options, cancellationToken);
        }

        // Render a prompt to select the skill to call.
        private async Task<DialogTurnResult> SelectSkillStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var skillType = ((FoundChoice)stepContext.Result).Value;

            // Create the PromptOptions from the skill configuration which contain the list of configured skills.
            const string messageText = "What skill would you like to call?";
            const string repromptMessageText = "That was not a valid choice, please select a valid skill.";

            var choices = _skillsConfig.Skills
                .Where(skill => skill.Key.StartsWith(skillType))
                .Select(skill => new Choice(skill.Value.Id))
                .ToList();

            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
                Style = ListStyle.SuggestedAction,
                Choices = choices
            };

            // Prompt the user to select a skill.
            return await stepContext.PromptAsync("SkillPrompt", options, cancellationToken);
        }

        // Render a prompt to select the begin action for the skill.
        private async Task<DialogTurnResult> SelectSkillActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the skill info based on the selected skill.
            var selectedSkillId = ((FoundChoice)stepContext.Result).Value;
            var deliveryMode = stepContext.Values[_deliveryMode].ToString();
            var v3Bots = new List<string> { "EchoSkillBotDotNetV3", "EchoSkillBotJSV3" };

            // Exclude v3 bots from ExpectReplies
            if (deliveryMode == DeliveryModes.ExpectReplies && v3Bots.Contains(selectedSkillId))
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("V3 Bots do not support 'expectReplies' delivery mode."), cancellationToken);

                // Restart setup dialog
                return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
            }

            var selectedSkill = _skillsConfig.Skills.FirstOrDefault(keyValuePair => keyValuePair.Value.Id == selectedSkillId).Value;

            // Remember the skill selected by the user.
            stepContext.Values[_selectedSkillKey] = selectedSkill;

            // Create the PromptOptions with the actions supported by the selected skill.
            var messageText = $"Select an action # to send to **{selectedSkill.Id}**.\n\nOr just type in a message and it will be forwarded to the skill as a message activity.";
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                Choices = selectedSkill.GetActions().Select(action => new Choice(action)).ToList()
            };

            // Prompt the user to select a skill action.
            return await stepContext.PromptAsync("SkillActionPrompt", options, cancellationToken);
        }

        // This validator defaults to Message if the user doesn't select an existing option.
        private Task<bool> SkillActionPromptValidator(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            if (!promptContext.Recognized.Succeeded)
            {
                // Assume the user wants to send a message if an item in the list is not selected.
                promptContext.Recognized.Value = new FoundChoice { Value = JustForwardTheActivity };
            }

            return Task.FromResult(true);
        }

        // Starts the SkillDialog based on the user's selections.
        private async Task<DialogTurnResult> CallSkillActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var selectedSkill = (BotFrameworkSkill)stepContext.Values[_selectedSkillKey];

            var skillActivity = CreateBeginActivity(stepContext.Context, selectedSkill.Id, ((FoundChoice)stepContext.Result).Value);

            // Create the BeginSkillDialogOptions and assign the activity to send.
            var skillDialogArgs = new BeginSkillDialogOptions { Activity = skillActivity };

            var deliveryMode = stepContext.Values[_deliveryMode].ToString();

            if (deliveryMode == DeliveryModes.ExpectReplies)
            {
                skillDialogArgs.Activity.DeliveryMode = DeliveryModes.ExpectReplies;
            }

            // Save active skill in state.
            await _activeSkillProperty.SetAsync(stepContext.Context, selectedSkill, cancellationToken);

            if (skillActivity.Name == "Sso")
            {
                // Special case, we start the SSO dialog to prepare the host to call the skill.
                return await stepContext.BeginDialogAsync(nameof(SsoDialog), cancellationToken: cancellationToken);
            }

            // Start the skillDialog instance with the arguments. 
            return await stepContext.BeginDialogAsync(selectedSkill.Id, skillDialogArgs, cancellationToken);
        }

        // The SkillDialog has ended, render the results (if any) and restart MainDialog.
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var activeSkill = await _activeSkillProperty.GetAsync(stepContext.Context, () => null, cancellationToken);

            // Check if the skill returned any results and display them.
            if (stepContext.Result != null)
            {
                var message = $"Skill \"{activeSkill.Id}\" invocation complete.";
                message += $" Result: {JsonConvert.SerializeObject(stepContext.Result)}";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(message, message, inputHint: InputHints.IgnoringInput), cancellationToken: cancellationToken);
            }

            // Clear the delivery mode selected by the user.
            stepContext.Values[_deliveryMode] = null;

            // Clear the skill selected by the user.
            stepContext.Values[_selectedSkillKey] = null;

            // Clear active skill in state.
            await _activeSkillProperty.DeleteAsync(stepContext.Context, cancellationToken);

            // Restart the main dialog with a different message the second time around.
            return await stepContext.ReplaceDialogAsync(InitialDialogId, $"Done with \"{activeSkill.Id}\". \n\n What delivery mode would you like to use?", cancellationToken);
        }

        // Helper method that creates and adds SkillDialog instances for the configured skills.
        private void AddSkillDialogs(ConversationState conversationState, SkillConversationIdFactoryBase conversationIdFactory, SkillHttpClient skillClient, SkillsConfiguration skillsConfig, string botId)
        {
            foreach (var skillInfo in _skillsConfig.Skills.Values)
            {
                // Create the dialog options.
                var skillDialogOptions = new SkillDialogOptions
                {
                    BotId = botId,
                    ConversationIdFactory = conversationIdFactory,
                    SkillClient = skillClient,
                    SkillHostEndpoint = skillsConfig.SkillHostEndpoint,
                    ConversationState = conversationState,
                    Skill = skillInfo
                };

                // Add a SkillDialog for the selected skill.
                AddDialog(new SkillDialog(skillDialogOptions, skillInfo.Id));
            }
        }

        // Helper method to create the activity to be sent to the DialogSkillBot using selected type and values.
        private Activity CreateBeginActivity(ITurnContext turnContext, string skillId, string selectedOption)
        {
            if (selectedOption.Equals(JustForwardTheActivity, StringComparison.CurrentCultureIgnoreCase))
            {
                // Note message activities also support input parameters but we are not using them in this example.
                // Return a deep clone of the activity so we don't risk altering the original one 
                return ObjectPath.Clone(turnContext.Activity);
            }

            // Get the begin activity from the skill instance.
            var activity = _skillsConfig.Skills[skillId].CreateBeginActivity(selectedOption);

            // We are manually creating the activity to send to the skill; ensure we add the ChannelData and Properties 
            // from the original activity so the skill gets them.
            // Note: this is not necessary if we are just forwarding the current activity from context. 
            activity.ChannelData = turnContext.Activity.ChannelData;
            activity.Properties = turnContext.Activity.Properties;

            return activity;
        }
    }
}
