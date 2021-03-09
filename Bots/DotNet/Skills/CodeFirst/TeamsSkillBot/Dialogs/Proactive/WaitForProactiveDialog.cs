// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace Microsoft.BotFrameworkFunctionalTests.TeamsSkillBot.Dialogs.Proactive
{
    public class WaitForProactiveDialog : Dialog
    {
        // Message to send to users when the bot receives a Conversation Update event
        private const string NotifyMessage = "Navigate to {0}api/notify?message={1} to proactively message everyone who has previously messaged this bot.";

        private readonly Uri _serverUrl;
        
        public WaitForProactiveDialog(IHttpContextAccessor httpContextAccessor)
        {
            _serverUrl = new Uri($"{httpContextAccessor.HttpContext.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host.Value}");
        }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = new CancellationToken())
        {
            await dc.Context.SendActivityAsync(MessageFactory.Text(string.Format(NotifyMessage, _serverUrl, Guid.NewGuid())), cancellationToken);
            return EndOfTurn;
        }

        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = new CancellationToken())
        {
            if (dc.Context.Activity.Type == ActivityTypes.Event && dc.Context.Activity.Name == ActivityEventNames.ContinueConversation)
            {
                // The continue conversation activity comes from the ProactiveController when the notification is received
                await dc.Context.SendActivityAsync("We received a proactive message, ending the dialog", cancellationToken: cancellationToken);

                // End the dialog so the host gets an EoC
                return new DialogTurnResult(DialogTurnStatus.Complete);
            }

            // Keep waiting for a call to the ProactiveController.
            await dc.Context.SendActivityAsync($"We are waiting for a proactive message. {string.Format(NotifyMessage, _serverUrl, Guid.NewGuid())}", cancellationToken: cancellationToken);

            return EndOfTurn;
        }
    }
}
