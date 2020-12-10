// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Integration.AspNet.Core.Skills;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs
{
    /// <summary>
    /// The main dialog for this bot. It uses a <see cref="SkillDialog"/> to call skills.
    /// </summary>
    public class MainDialog : ComponentDialog
    {
        public const string ActiveSkillPropertyName = "activeSkillProperty";
        private const string JustForwardTheActivity = "JustForwardTurnContext.Activity";
        
        private readonly IStatePropertyAccessor<BotFrameworkSkill> _activeSkillProperty;

        private readonly string _selectedSkillKey = $"{typeof(MainDialog).FullName}.SelectedSkillKey";

        // Dependency injection uses this constructor to instantiate MainDialog.
        public MainDialog(ConversationState conversationState, IConfiguration configuration, IHttpClientFactory clientFactory)
            : base(nameof(MainDialog))
        {
            var botId = configuration.GetSection(MicrosoftAppCredentials.MicrosoftAppIdKey)?.Value;

            if (conversationState == null)
            {
                throw new ArgumentNullException(nameof(conversationState));
            }

            // Create and add SkillDialog instances for the configured skills.
            //AddSkillDialogs(conversationState, conversationIdFactory, skillClient, botId);

            // Add ChoicePrompt to render available skills.
            AddDialog(new ChoicePrompt("SkillPrompt"));

            AddDialog(new ChoicePrompt("CardPrompt"));

            AddDialog(new CardDialog(configuration, clientFactory));

            AddDialog(new AttachmentDialog());

            // Add ChoicePrompt to render skill actions.
            AddDialog(new ChoicePrompt("SkillActionPrompt"));

            // Add main waterfall dialog for this bot.
            var waterfallSteps = new WaterfallStep[]
            {
                SelectSkillStepAsync,
                SelectSkillActionStepAsync,
                FinalStepAsync
            };
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            // Create state property to track the active skill.
            _activeSkillProperty = conversationState.CreateProperty<BotFrameworkSkill>(ActiveSkillPropertyName);

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
            if (activeSkill != null && activity.Type == ActivityTypes.Message && activity.Text.Equals("abort", StringComparison.CurrentCultureIgnoreCase))
            {
                // Cancel all dialogs when the user says abort.
                // The SkillDialog automatically sends an EndOfConversation message to the skill to let the
                // skill know that it needs to end its current dialogs, too.
                await innerDc.CancelAllDialogsAsync(cancellationToken);
                return await innerDc.ReplaceDialogAsync(InitialDialogId, "Canceled! \n\n What skill would you like to call?", cancellationToken);
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        // Render a prompt to select the skill to call.
        private async Task<DialogTurnResult> SelectSkillStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Create the PromptOptions from the skill configuration which contain the list of configured skills.
            var messageText = "What skill would you like to call?";
            var repromptMessageText = "That was not a valid choice, please select a valid skill.";
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
                Choices = new List<Choice> { new Choice("Card"), new Choice("Proactive"), new Choice("Attachment") }
            };

            // Prompt the user to select a skill.
            return await stepContext.PromptAsync("SkillPrompt", options, cancellationToken);
        }

        // Render a prompt to select the begin action for the skill.
        private async Task<DialogTurnResult> SelectSkillActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the skill info based on the selected skill.
            var selectedSkillId = ((FoundChoice)stepContext.Result).Value;

            switch (selectedSkillId)
            {
                case "Card":
                    return await stepContext.BeginDialogAsync(nameof(CardDialog), null, cancellationToken);

                case "Proactive":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Visit localhost:[PORT]/api/notify to receive a proactive message."), cancellationToken);
                    break;

                case "Attachment":
                    return await stepContext.BeginDialogAsync(nameof(AttachmentDialog), null, cancellationToken);

                default:
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("command unrecognized."), cancellationToken);
                    break;
            }

            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text("text", "text", InputHints.ExpectingInput),
                Choices = new List<Choice> { new Choice("Card") }
            };

            // Prompt the user to select a skill action.
            return await stepContext.ReplaceDialogAsync(InitialDialogId, $"Done with \"{selectedSkillId}\". \n\n What skill would you like to call?", cancellationToken);
        }

        private async Task<DialogTurnResult> CardSelectStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the skill info based on the selected skill.
            var selectedSkillId = ((FoundChoice)stepContext.Result).Value;

            await stepContext.Context.SendActivityAsync(MessageFactory.Text("hi"), cancellationToken);
            return new DialogTurnResult(DialogTurnStatus.Complete);
        }

        private async Task<DialogTurnResult> SendCards(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var messageText = "What card do you want?";
            var repromptMessageText = "That was not a valid choice, please select a valid skill.";
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
                Choices = new List<Choice> { new Choice("Hero"), new Choice("Receipt") }
            };

            // Prompt the user to select a skill.
            return await stepContext.PromptAsync("CardPrompt", options, cancellationToken);
        }

        /*
        // Starts the SkillDialog based on the user's selections.
        private async Task<DialogTurnResult> CallSkillActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var selectedSkill = (BotFrameworkSkill)stepContext.Values[_selectedSkillKey];

            var skillActivity = CreateBeginActivity(stepContext.Context, selectedSkill.Id, ((FoundChoice)stepContext.Result).Value);

            // Create the BeginSkillDialogOptions and assign the activity to send.
            var skillDialogArgs = new BeginSkillDialogOptions { Activity = skillActivity };

            // Comment or uncomment this line if you need to enable or disabled buffered replies.
            // skillDialogArgs.Activity.DeliveryMode = DeliveryModes.ExpectReplies;

            // Save active skill in state.
            await _activeSkillProperty.SetAsync(stepContext.Context, selectedSkill, cancellationToken);

            // Start the skillDialog instance with the arguments. 
            return await stepContext.BeginDialogAsync(selectedSkill.Id, skillDialogArgs, cancellationToken);
        }*/

        // The SkillDialog has ended, render the results (if any) and restart MainDialog.
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Restart the main dialog with a different message the second time around.
            return await stepContext.ReplaceDialogAsync(InitialDialogId, $"Done with done. \n\n What skill would you like to call?", cancellationToken);
        }

        // Helper method that creates and adds SkillDialog instances for the configured skills.
        private void AddSkillDialogs(ConversationState conversationState, SkillConversationIdFactoryBase conversationIdFactory, SkillHttpClient skillClient, string botId)
        {
            //foreach (var skillInfo in _skillsConfig.Skills.Values)
            //{
            //    // Create the dialog options.
            //    var skillDialogOptions = new SkillDialogOptions
            //    {
            //        BotId = botId,
            //        ConversationIdFactory = conversationIdFactory,
            //        SkillClient = skillClient,
            //        SkillHostEndpoint = skillsConfig.SkillHostEndpoint,
            //        ConversationState = conversationState,
            //        Skill = skillInfo
            //    };

            //    // Add a SkillDialog for the selected skill.
            //    AddDialog(new SkillDialog(skillDialogOptions, skillInfo.Id));
            //}
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
            var activity = new Activity();

            // We are manually creating the activity to send to the skill; ensure we add the ChannelData and Properties 
            // from the original activity so the skill gets them.
            // Note: this is not necessary if we are just forwarding the current activity from context. 
            activity.ChannelData = turnContext.Activity.ChannelData;
            activity.Properties = turnContext.Activity.Properties;

            return activity;
        }
    }
}
