// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.BotFrameworkFunctionalTests.MultiTurnDialogSkill.Bots;

namespace Microsoft.BotFrameworkFunctionalTests.EchoSkillBot.Controllers
{
    /// <summary>
    /// This ASP Controller is created to handle a request. Dependency Injection will provide the Adapter and IBot implementation at runtime.
    /// Multiple different IBot implementations running at different endpoints can be achieved by specifying a more specific type for the bot constructor argument.
    /// </summary>
    [Route("api/messages")]
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly IBot _bot;
        private readonly DialogBot<UserProfileDialog> _dialogBot;

        /// <summary>
        /// Initializes a new instance of the <see cref="BotController"/> class.
        /// </summary>
        /// <param name="adapter">Adapter for the BotController.</param>
        /// <param name="bot">Bot for the BotController.</param>
        public BotController(IBotFrameworkHttpAdapter adapter, IBot bot, DialogBot<UserProfileDialog> dialogBot)
        {
            _adapter = adapter;
            _bot = bot;
            _dialogBot = dialogBot;
        }

        /// <summary>
        /// Processes an HttpPost request.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [HttpPost]
        public async Task PostAsync()
        {
            await _adapter.ProcessAsync(Request, Response, _dialogBot);
            // await _adapter.ProcessAsync(Request, Response, _bot);
        }
    }
}
