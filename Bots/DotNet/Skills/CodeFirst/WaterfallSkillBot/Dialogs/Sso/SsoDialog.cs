// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Sso
{
    public class SsoDialog : ComponentDialog
    {
        private readonly ILogger _logger;

        public SsoDialog(IConfiguration configuration)
            : base(nameof(SsoDialog))
        {
            AddDialog(new SignInDialog(configuration));
            AddDialog(new SignOutDialog(configuration));
            AddDialog(new DisplayTokenDialog(configuration));

            AddDialog(new ChoicePrompt("ActionStepPrompt"));
            AddDialog(new ChoicePrompt("FinalStepPrompt"));

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
                Choices = new List<Choice> { new Choice("Login"), new Choice("Logout"), new Choice("Show token") }
            };

            // Prompt the user to select a skill.
            return await stepContext.PromptAsync("ActionStepPrompt", options, cancellationToken);
        }

        private async Task<DialogTurnResult> HandleActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var action = ((FoundChoice)stepContext.Result).Value.ToLowerInvariant();
            DialogTurnResult dialogResult;
            
            if (action == "login")
            {
                dialogResult = await stepContext.BeginDialogAsync(nameof(SignInDialog), null, cancellationToken);
            } 
            else if (action == "logout")
            {
                dialogResult = await stepContext.BeginDialogAsync(nameof(SignOutDialog), null, cancellationToken);
            }
            else if (action == "show token")
            {
                dialogResult = await stepContext.BeginDialogAsync(nameof(DisplayTokenDialog), null, cancellationToken);
            }
            else
            {
                dialogResult = new DialogTurnResult(DialogTurnStatus.Complete);
            }

            return dialogResult;
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

            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Ending conversation with the skill. Heading back to parent."));
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}
