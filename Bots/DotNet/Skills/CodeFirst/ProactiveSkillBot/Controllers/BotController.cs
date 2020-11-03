// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;

namespace Microsoft.BotBuilderSamples.ProactiveSkillBot.Controllers
{
    // This ASP Controller is created to handle a request. Dependency Injection will provide the Adapter and IBot
    // implementation at runtime. Multiple different IBot implementations running at different endpoints can be
    // achieved by specifying a more specific type for the bot constructor argument.
    [Route("api/messages")]
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly IBot _bot;

        public BotController(IBotFrameworkHttpAdapter adapter, IBot bot)
        {
            _adapter = adapter;
            _bot = bot;
        }

        /// <summary>
        /// Gets the server URL where the bot is hosted.
        /// </summary>
        /// <remarks>
        /// This is just for testing, it allows use to return a card with a clickable URL so we can invoke the notify. 
        /// </remarks>
        /// <value>
        /// The server URL where the bot is hosted.
        /// </value>
        public static Uri ServerUrl { get; private set; }

        [HttpPost]
        public async Task PostAsync()
        {
            if (ServerUrl == null)
            {
                ServerUrl = new Uri($"{Request.Scheme}://{Request.Host.Value}");
            }

            // Delegate the processing of the HTTP POST to the adapter.
            // The adapter will invoke the bot.
            await _adapter.ProcessAsync(Request, Response, _bot);
        }
    }
}
