// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace Microsoft.BotFrameworkFunctionalTests.EchoSkillBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            var options = new MessageOptions
            {
                InputHint = InputHints.AcceptingInput
            };

            // Send an `endOfconversation` activity if the user cancels the skill.
            if (activity.Text.ToLower().Contains("end") || activity.Text.ToLower().Contains("stop"))
            {
                await context.SayAsync($"Ending conversation from the skill...", options: options);
                var endOfConversation = activity.CreateReply();
                endOfConversation.Type = ActivityTypes.EndOfConversation;
                endOfConversation.Code = EndOfConversationCodes.CompletedSuccessfully;
                endOfConversation.InputHint = InputHints.AcceptingInput;
                await context.PostAsync(endOfConversation);
            }
            else
            {
                await context.SayAsync($"Echo: {activity.Text}", options: options);
                await context.SayAsync($"Say \"end\" or \"stop\" and I'll end the conversation and back to the parent.", options: options);
            }

            context.Wait(MessageReceivedAsync);
        }
    }
}
