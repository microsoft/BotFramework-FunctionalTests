﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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

            // Send an `endOfconversation` activity if the user cancels the skill.
            if (activity.Text.ToLower().Contains("end") || activity.Text.ToLower().Contains("stop"))
            {
                await context.PostAsync($"Ending conversation from the skill...");
                var endOfConversation = activity.CreateReply();
                endOfConversation.Type = ActivityTypes.EndOfConversation;
                endOfConversation.Code = EndOfConversationCodes.UserCancelled;
                await context.PostAsync(endOfConversation);
            }
            else
            {
                await context.PostAsync($"Echo: {activity.Text}");
                await context.PostAsync($"Say 'end' or 'stop' and I'll end the conversation and back to the parent.");
            }

            context.Wait(MessageReceivedAsync);
        }
    }
}
