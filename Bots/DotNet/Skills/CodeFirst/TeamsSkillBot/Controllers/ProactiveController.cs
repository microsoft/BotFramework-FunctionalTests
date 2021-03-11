// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.BotFrameworkFunctionalTests.TeamsSkillBot.Dialogs;
using Microsoft.BotFrameworkFunctionalTests.TeamsSkillBot.Dialogs.Proactive;

namespace Microsoft.BotFrameworkFunctionalTests.TeamsSkillBot.Controllers
{
    [Route("api/notify")]
    [ApiController]
    public class ProactiveController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly ConcurrentDictionary<string, ContinuationParameters> _continuationParameters;
        private readonly ConversationState _conversationState;
        private readonly ActivityRouterDialog _mainDialog;

        public ProactiveController(ConversationState conversationState, ActivityRouterDialog mainDialog, IBotFrameworkHttpAdapter adapter, ConcurrentDictionary<string, ContinuationParameters> continuationParameters)
        {
            _conversationState = conversationState;
            _adapter = adapter;
            _continuationParameters = continuationParameters;
            _mainDialog = mainDialog;
        }

        // Note: in production scenarios, this controller should be secured.
        public async Task<IActionResult> Get(string message)
        {
            if (!_continuationParameters.Any())
            {
                // Let the caller know a proactive messages have been sent
                return new ContentResult
                {
                    Content = "<html><body><h1>No messages sent</h1> <br/> There are no conversations registered to receive proactive messages.</body></html>",
                    ContentType = "text/html",
                    StatusCode = (int)HttpStatusCode.OK,
                };
            }

            Exception exception = null;
            try
            {
                // Clone the dictionary so we can modify it as we respond.
                var continuationClone = new ConcurrentDictionary<string, ContinuationParameters>(_continuationParameters);
                foreach (var continuationParameters in continuationClone)
                {
                    var continuationItem = continuationParameters.Value;

                    async Task BotCallback(ITurnContext context, CancellationToken cancellationToken)
                    {
                        await context.SendActivityAsync($"Got proactive message with value: {message}", cancellationToken: cancellationToken);

                        // If we didn't have dialogs we could remove the code below, but we want to continue the dialog to clear the 
                        // dialog stack.
                        // Run the main dialog to continue WaitForProactiveDialog and send an EndOfConversation when that one is done.
                        // ContinueDialogAsync in WaitForProactiveDialog will get a ContinueConversation event when this is called.
                        await _mainDialog.RunAsync(context, _conversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);

                        // Save any state changes so the dialog stack is persisted.
                        await _conversationState.SaveChangesAsync(context, false, cancellationToken);
                    }

                    // Forget the reference so we don't try to start the dialog again once is done.
                    _continuationParameters.TryRemove(continuationParameters.Key, out _);

                    // Continue the conversation with the proactive message
                    await ((BotFrameworkAdapter)_adapter).ContinueConversationAsync((ClaimsIdentity)continuationItem.ClaimsIdentity, continuationItem.ConversationReference, continuationItem.OAuthScope, BotCallback, default);
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            // Let the caller know a proactive messages have been sent
            return new ContentResult
            {
                Content = $"<html><body><h1>Proactive messages have been sent</h1> <br/> Timestamp: {DateTime.Now} <br /> Exception: {exception}</body></html>",
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
            };
        }
    }
}
