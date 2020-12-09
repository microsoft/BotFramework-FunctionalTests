// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs;
using Microsoft.Extensions.Configuration;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Controllers
{
    [Route("api/notify")]
    [ApiController]
    public class ProactiveController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly string _appId;
        private readonly ConcurrentDictionary<string, ConversationReference> _conversationReferences;
        private readonly ConversationState _conversationState;
        private readonly ActivityRouterDialog _mainDialog;

        public ProactiveController(ConversationState conversationState, ActivityRouterDialog mainDialog, IBotFrameworkHttpAdapter adapter, IConfiguration configuration, ConcurrentDictionary<string, ConversationReference> conversationReferences)
        {
            _conversationState = conversationState;
            _adapter = adapter;
            _conversationReferences = conversationReferences;
            _appId = configuration["MicrosoftAppId"];
            if (string.IsNullOrWhiteSpace(_appId))
            {
                // If the channel is the Emulator, and authentication is not in use,
                // the AppId will be null.  We the AnonymousSkillAppId for this case only.
                // This is not required for production, since the AppId will have a value.
                _appId = AuthenticationConstants.AnonymousSkillAppId;
            }

            _mainDialog = mainDialog;
        }

        public async Task<IActionResult> Get(string message)
        {
            Exception exception = null;
            try
            {
                foreach (var conversationReference in _conversationReferences.Values)
                {
                    async Task BotCallback(ITurnContext context, CancellationToken cancellationToken)
                    {
                        await context.SendActivityAsync($"Get proactive message with value: {message}", cancellationToken: cancellationToken);

                        // Run the main dialog to continue WaitForProactiveDialog and send an EndOfConversation when that one is done.
                        // ContinueDialogAsync in WaitForProactiveDialog will get a ContinueConversation event when this is called.
                        await _mainDialog.RunAsync(context, _conversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
                    }

                    await ((BotAdapter)_adapter).ContinueConversationAsync(_appId, conversationReference, BotCallback, default);
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            // Let the caller know a proactive messages have been sent
            return new ContentResult
            {
                Content = $"<html><body><h1>Proactive messages have been sent</h1> <br/> Timestamp: {DateTime.Now} <br />AppId: {_appId} <br/> Exception: {exception}</body></html>",
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
            };
        }
    }
}
