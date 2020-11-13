// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotFrameworkFunctionalTests.WeatherSkillBot.Bots
{
    public class WeatherBot : ActivityHandler
    {
        private readonly HttpClient _client;
        private readonly Uri _url;

        public WeatherBot()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("User-Agent", "Microsoft Skill Sample");
            _url = new Uri("https://api.weather.gov/gridpoints/SEW/131,69/forecast");
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity.Text.Contains("end") || turnContext.Activity.Text.Contains("stop"))
            {
                // Send End of conversation at the end.
                var messageText = "ending conversation from the skill...";
                await turnContext.SendActivityAsync(MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput), cancellationToken);
                var endOfConversation = Activity.CreateEndOfConversationActivity();
                endOfConversation.Code = EndOfConversationCodes.CompletedSuccessfully;
                await turnContext.SendActivityAsync(endOfConversation, cancellationToken);
            }
            else
            {
                string messageText;
                var weatherReport = await _client.GetAsync(_url);
                if (weatherReport.IsSuccessStatusCode)
                {
                    var obj = JObject.Parse(await weatherReport.Content.ReadAsStringAsync());
                    var report = JsonConvert.DeserializeObject<List<SingleDay>>(JsonConvert.SerializeObject(obj["properties"]["periods"]))[0];
                    messageText = $"{report.Name} the weather in Redmond is {report.ShortForecast.ToLower()} with a temperature of {report.Temperature} degrees.";
                    await turnContext.SendActivityAsync(MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput), cancellationToken);
                }
                else
                {
                    messageText = "Something went wrong with the weather API.";
                    await turnContext.SendActivityAsync(MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput), cancellationToken);
                }

                messageText = "Say \"end\" or \"stop\" and I'll end the conversation and back to the parent.";
                await turnContext.SendActivityAsync(MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput), cancellationToken);
            }
        }

        protected override Task OnEndOfConversationActivityAsync(ITurnContext<IEndOfConversationActivity> turnContext, CancellationToken cancellationToken)
        {
            // This will be called if the root bot is ending the conversation.  Sending additional messages should be
            // avoided as the conversation may have been deleted.
            // Perform cleanup of resources if needed.
            return Task.CompletedTask;
        }
    }
}
