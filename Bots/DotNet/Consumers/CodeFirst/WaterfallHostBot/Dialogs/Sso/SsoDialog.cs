// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Integration.AspNet.Core.Skills;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallHostBot.Dialogs.Sso
{
    public class SsoDialog : ComponentDialog
    {
        public SsoDialog(ConversationState conversationState, SkillsConfiguration skillsConfig, SkillHttpClient skillClient, IConfiguration configuration, SkillConversationIdFactoryBase conversationIdFactory)
            : base(nameof(SsoDialog))
        {
            AddDialog(new SignInDialog(configuration));
            AddDialog(new SignOutDialog(configuration));
            AddDialog(new DisplayTokenDialog(configuration));

            var botId = configuration.GetSection("MicrosoftAppId")?.Value;

            AddDialog(new ChoicePrompt("ActionStepPrompt"));
            AddDialog(new ChoicePrompt("FinalStepPrompt"));

            skillsConfig.Skills.TryGetValue("WaterfallSkillBot", out var skill);
            AddDialog(new SkillDialog(
                new SkillDialogOptions()
                {
                    BotId = botId,
                    ConversationIdFactory = conversationIdFactory,
                    ConversationState = conversationState,
                    Skill = skill,
                    SkillClient = skillClient,
                    SkillHostEndpoint = skillsConfig.SkillHostEndpoint
                },
                nameof(SkillDialog)));

            var waterfallSteps = new WaterfallStep[]
            {
                PromptActionStepAsync,
                HandleActionStepAsync,
                PromptFinalStepAsync,
                HandleFinalStepAsync
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PromptActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var messageText = "What do you want to do?";
            var repromptMessageText = "That was not a valid choice, please select a valid choice.";
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
                Choices = new List<Choice> { new Choice("Login"), new Choice("Logout"), new Choice("Show token"), new Choice("Call Skill"), new Choice("End") }
            };

            // Prompt the user to select a skill.
            return await stepContext.PromptAsync("ActionStepPrompt", options, cancellationToken);
        }

        private async Task<DialogTurnResult> HandleActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var action = ((FoundChoice)stepContext.Result).Value.ToLowerInvariant();

            switch (action)
            {
                case "login":
                    return await stepContext.BeginDialogAsync(nameof(SignInDialog), null, cancellationToken);       

                case "logout":
                    return await stepContext.BeginDialogAsync(nameof(SignOutDialog), null, cancellationToken);

                case "show token":
                    return await stepContext.BeginDialogAsync(nameof(DisplayTokenDialog), null, cancellationToken);

                case "call skill":
                    stepContext.Context.Activity.Type = ActivityTypes.Event;
                    stepContext.Context.Activity.Name = "Sso";
                    return await stepContext.BeginDialogAsync(nameof(SkillDialog), new BeginSkillDialogOptions() { Activity = stepContext.Context.Activity }, cancellationToken);

                default:
                    // This should never be hit since the previous prompt validates the choice
                    return new DialogTurnResult(DialogTurnStatus.Complete);
            }
        }

        private async Task<DialogTurnResult> PromptFinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var messageText = "What do you want to do?";
            var repromptMessageText = "That was not a valid choice, please select a valid choice.";
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
                Choices = new List<Choice> { new Choice("Another action"), new Choice("End") }
            };

            // Prompt the user to select a skill.
            return await stepContext.PromptAsync("FinalStepPrompt", options, cancellationToken);
        }

        private async Task<DialogTurnResult> HandleFinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var action = ((FoundChoice)stepContext.Result).Value.ToLowerInvariant();

            if (action == "another action")
            {
                return await stepContext.ReplaceDialogAsync(InitialDialogId, "What do you want to do?", cancellationToken);
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}
