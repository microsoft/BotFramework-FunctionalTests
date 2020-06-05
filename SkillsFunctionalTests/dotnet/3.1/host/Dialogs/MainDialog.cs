// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
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
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotFrameworkFunctionalTests.SimpleHostBot.Dialogs
{
    /// <summary>
    /// The main dialog for this bot. It uses a <see cref="SkillDialog"/> to call skills.
    /// </summary>
    public class MainDialog : ComponentDialog
    {
        public static readonly string ActiveSkillPropertyName = $"{typeof(MainDialog).FullName}.ActiveSkillProperty";
        private const string EchoSkill = "EchoSkill";
        private const string SkillAction_Message = "Message";
        private const string DialogSkill = "DialogSkill";
        private readonly IStatePropertyAccessor<BotFrameworkSkill> _activeSkillProperty;
        private readonly string _selectedSkillKey = $"{typeof(MainDialog).FullName}.SelectedSkillKey";
        private readonly SkillsConfiguration _skillsConfig;
        public MainDialog(ConversationState conversationState, SkillConversationIdFactoryBase conversationIdFactory, SkillHttpClient skillClient, SkillsConfiguration skillsConfig, IConfiguration configuration)
            : base(nameof(MainDialog))
        {
            var botId = configuration.GetSection(MicrosoftAppCredentials.MicrosoftAppIdKey)?.Value;
            if (string.IsNullOrWhiteSpace(botId))
            {
                throw new ArgumentException($"{MicrosoftAppCredentials.MicrosoftAppIdKey} is not in configuration");
            }

            _skillsConfig = skillsConfig ?? throw new ArgumentNullException(nameof(skillsConfig));

            if (skillClient == null)
            {
                throw new ArgumentNullException(nameof(skillClient));
            }

            if (conversationState == null)
            {
                throw new ArgumentNullException(nameof(conversationState));
            }

            // Register the tangent
            AddDialog(new TangentDialog());

            // Use helper method to add SkillDialog instances for the configured skills.
            AddSkillDialogs(conversationState, conversationIdFactory, skillClient, skillsConfig, botId);

            // Add ChoicePrompt to render available skills.
            AddDialog(new ChoicePrompt("SkillPrompt"));

            // Add main waterfall dialog for this bot.
            var waterfallSteps = new WaterfallStep[]
            {
                SelectSkillStepAsync,
                CallSkillStepAsync,
                FinalStepAsync
            };
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            // Create state property to track the active skill.
            _activeSkillProperty = conversationState.CreateProperty<BotFrameworkSkill>(ActiveSkillPropertyName);

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            // Sample to test a tangent when in the middle of a skill conversation.
            var activeSkill = await _activeSkillProperty.GetAsync(innerDc.Context, () => null, cancellationToken);
            var activity = innerDc.Context.Activity;
            if (activeSkill != null && activity.Type == ActivityTypes.Message && activity.Text.Equals("tangent", StringComparison.CurrentCultureIgnoreCase))
            {
                // Start tangent.
                return await innerDc.BeginDialogAsync(nameof(TangentDialog), cancellationToken: cancellationToken);
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        // Render a prompt to select the skill to call.
        private async Task<DialogTurnResult> SelectSkillStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Create the PromptOptions from the skill configuration which contain the list of configured skills.
            var messageText = stepContext.Options?.ToString() ?? "What skill would you like to call?";
            var repromptMessageText = "That was not a valid choice, please select a valid skill.";
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
                Choices = _skillsConfig.Skills.Select(skill => new Choice(skill.Value.Id)).ToList()
            };

            // Prompt the user to select a skill.
            return await stepContext.PromptAsync("SkillPrompt", options, cancellationToken);
        }
        
        // Starts the SkillDialog based on the user's selections.
        private async Task<DialogTurnResult> CallSkillStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the skill info based on the selected skill.
            var selectedSkillId = ((FoundChoice)stepContext.Result).Value;
            var selectedSkill = _skillsConfig.Skills.FirstOrDefault(s => s.Value.Id == selectedSkillId).Value;

            // Remember the skill selected by the user.
            stepContext.Values[_selectedSkillKey] = selectedSkill;

            Activity skillActivity;
            switch (selectedSkill.Id)
            {
                case "EchoSkillBot":
                    skillActivity = CreateActivityToSendSkill(SkillAction_Message, stepContext.Context);

                    break;
                case DialogSkill:
                    skillActivity = CreateActivityToSendSkill(((FoundChoice)stepContext.Result).Value, stepContext.Context);
                    break;
                default:
                    throw new Exception($"Unknown target skill id: {selectedSkill.Id}.");
            }

            // Create the BeginSkillDialogOptions and assign the activity to send.
            var skillDialogArgs = new BeginSkillDialogOptions { Activity = skillActivity };

            // Comment or uncomment this line if you need to enable or disabled buffered replies.
            //skillDialogArgs.Activity.DeliveryMode = DeliveryModes.ExpectReplies;

            // Save active skill in state.
            await _activeSkillProperty.SetAsync(stepContext.Context, selectedSkill, cancellationToken);

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

            // Clear the skill selected by the user.
            stepContext.Values[_selectedSkillKey] = null;

            // Clear active skill in state.
            await _activeSkillProperty.DeleteAsync(stepContext.Context, cancellationToken);

            // Restart the main dialog with a different message the second time around.
            // return await stepContext.ReplaceDialogAsync(InitialDialogId, $"Done with \"{activeSkill.Id}\". \n\n What skill would you like to call?", cancellationToken);
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
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

        // TODO: determine what the below comment should be
        // Helper method to create the activity to be sent to the DialogSkillBot using selected type and values.
        private Activity CreateActivityToSendSkill(string selectedOption, ITurnContext turnContext)
        {
            // Note: in a real bot, the dialogArgs will be created dynamically based on the conversation
            // and what each action requires; here we hardcode the values to make things simpler.

            // Just forward the message activity to the skill with whatever the user said. 
            if (selectedOption.Equals(SkillAction_Message, StringComparison.CurrentCultureIgnoreCase))
            {
                // Note message activities also support input parameters but we are not using them in this example.
                return turnContext.Activity;
            }

            Activity activity = null;
            // TODO: change comment
            // Send an event activity to the skill with "MakeUserProfile" in the name.
            if (selectedOption.Equals(DialogSkill, StringComparison.CurrentCultureIgnoreCase))
            {
                // activity = (Activity)Activity.CreateEventActivity();
                // activity.Name = DialogSkill;
                activity = (Activity)Activity.CreateMessageActivity();
                activity.Name = "dialog";
                // activity.ChannelData = new Dictionary<string, object> { ["activeSkillDialog"] = "multiTurnDialog" };
            }

            // Send an event activity to the skill with "EchoSkillBot" in the name.
            if (selectedOption.Equals(EchoSkill, StringComparison.CurrentCultureIgnoreCase))
            {
                // activity = (Activity)Activity.CreateEventActivity();
                activity = (Activity)Activity.CreateMessageActivity();
                activity.Name = EchoSkill;
                return activity;
            }

            if (activity == null)
            {
                throw new Exception($"Unable to create dialogArgs for \"{selectedOption}\".");
            }

            // We are manually creating the activity to send to the skill; ensure we add the ChannelData and Properties 
            // from the original activity so the skill gets them.
            // Note: this is not necessary if we are just forwarding the current activity from context. 
            activity.ChannelData = turnContext.Activity.ChannelData;
            activity.Properties = turnContext.Activity.Properties;

            return activity;
        }
    }
}