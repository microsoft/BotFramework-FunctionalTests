// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

namespace Microsoft.BotFrameworkFunctionalTests.TeamsSkillBot.Dialogs.Echo
{
    public class EchoDialog : ComponentDialog
    {
        public EchoDialog()
            : base(nameof(EchoDialog))
        {
            AddDialog(new TextPrompt("message"));
            AddDialog(new ChoicePrompt("choice"));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[] { PromptStepAsync, EchoStepAsync, FinalStepAsync }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PromptStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var msg = "Send me a message and I'll echo it back";
            var repromptMsg = "Please send me a message.";
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(msg, msg, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(repromptMsg, repromptMsg, InputHints.ExpectingInput),
            };
            
            return await stepContext.PromptAsync("message", options, cancellationToken);
        }

        private async Task<DialogTurnResult> EchoStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Echo: {stepContext.Context.Activity.Text}."), cancellationToken);
            
            var messageText = "Do you want to echo again?";
            var repromptMessageText = "Please make a valid choice.";
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
                Choices = new List<Choice> { new Choice("Yes"), new Choice("No") },
                Style = ListStyle.List
            };

            return await stepContext.PromptAsync("choice", options, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var choice = ((FoundChoice)stepContext.Result).Value.ToLowerInvariant();

            if (choice.Equals("yes"))
            {
                return await stepContext.ReplaceDialogAsync(InitialDialogId, "Send me a message and I'll echo it back.", cancellationToken);
            }
            
            return new DialogTurnResult(DialogTurnStatus.Complete);   
        }
    }
}
