// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs;
using Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Proactive;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Controllers
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
            Exception exception = null;
            try
            {
                foreach (var continuationParameters in _continuationParameters.Values)
                {
                    async Task BotCallback(ITurnContext context, CancellationToken cancellationToken)
                    {
                        await context.SendActivityAsync($"Got proactive message with value: {message}", cancellationToken: cancellationToken);

                        // If we didn't have dialogs we could remove the code below, but we want to continue the dialog to clear the 
                        // dialog stack.
                        // Run the main dialog to continue WaitForProactiveDialog and send an EndOfConversation when that one is done.
                        // ContinueDialogAsync in WaitForProactiveDialog will get a ContinueConversation event when this is called.
                        await _mainDialog.RunAsync(context, _conversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
                    }

                    await ((BotFrameworkAdapter)_adapter).ContinueConversationAsync((ClaimsIdentity)continuationParameters.ClaimsIdentity, continuationParameters.ConversationReference, continuationParameters.OAuthScope, BotCallback, default);
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
