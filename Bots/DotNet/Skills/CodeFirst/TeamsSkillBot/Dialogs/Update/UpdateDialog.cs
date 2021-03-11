// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;

namespace Microsoft.BotFrameworkFunctionalTests.TeamsSkillBot.Dialogs.Update
{
    public class UpdateDialog : ComponentDialog
    {
        private Dictionary<string, (string, int)> _updateTracker;
        private readonly HashSet<string> _updateSupported = new HashSet<string>
        {
            Channels.Msteams,
            Channels.Slack,
            Channels.Telegram
        };

        public UpdateDialog()
             : base(nameof(UpdateDialog))
        {
            _updateTracker = new Dictionary<string, (string, int)>();
            AddDialog(new ChoicePrompt("ChoicePrompt"));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[] { HandleUpdateDialog, FinalStepAsync }));
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> HandleUpdateDialog(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var channel = stepContext.Context.Activity.ChannelId;
            if (_updateSupported.Contains(channel))
            {
                if (_updateTracker.ContainsKey(stepContext.Context.Activity.Conversation.Id))
                {
                    var conversationId = stepContext.Context.Activity.Conversation.Id;
                    (string, int) tuple = _updateTracker[conversationId];
                    var activity = MessageFactory.Text($"This message has been updated {tuple.Item2} time(s).");
                    tuple.Item2 += 1;
                    activity.Id = tuple.Item1;
                    _updateTracker[conversationId] = tuple;
                    await stepContext.Context.UpdateActivityAsync(activity, cancellationToken);
                }
                else
                {
                    var id = await stepContext.Context.SendActivityAsync(MessageFactory.Text("Here is the original activity"), cancellationToken);
                    _updateTracker.Add(stepContext.Context.Activity.Conversation.Id, (id.Id, 1));
                }
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Delete is not supported in the {channel} channel."), cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Complete);
            }

            // Create the PromptOptions from the skill configuration which contain the list of configured skills.
            var messageText = "Do you want to update the activity again?";
            var repromptMessageText = "Please select a valid answer";
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText),
                RetryPrompt = MessageFactory.Text(repromptMessageText, repromptMessageText),
                Choices = new List<Choice> { new Choice("Yes"), new Choice("No") },
                Style = ListStyle.List
            };

            // Ask the user to enter their name.
            return await stepContext.PromptAsync("ChoicePrompt", options, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var choice = ((FoundChoice)stepContext.Result).Value.ToLowerInvariant();

            if (choice == "yes")
            {
                return await stepContext.ReplaceDialogAsync(InitialDialogId, "Do you want to update the activity?", cancellationToken);
            }
            else
            {
                _updateTracker.Remove(stepContext.Context.Activity.Conversation.Id);
                return new DialogTurnResult(DialogTurnStatus.Complete);
            }
        }
    }
}
