// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace CardSkill
{
    public class CardBot : ActivityHandler
    {
        public readonly string WelcomeMessage = "Send me one of these messages for a card: botActions, taskModule, submitAction, hero.";
        public readonly string PossibleCards = "botaction, taskmodule, submit, hero, thumbnail, receipt, signin, carousel, list, o365, file, uploadfile, animation, video, audio";
        internal readonly IHttpClientFactory _clientFactory;

        public CardBot(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        protected override async Task OnConversationUpdateActivityAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(MessageFactory.Text("Here is a carousel of most of the cards I have.")).ConfigureAwait(false);
            await turnContext.SendActivityAsync(MessageFactory.Attachment(CommandHandler.ListOfAllCards)).ConfigureAwait(false);
            await turnContext.SendActivityAsync(MessageFactory.Text($"You can send me the following messages to see all cards: {PossibleCards}"));
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity.Text != null)
            {
                if (turnContext.Activity.Text.Contains("end") || turnContext.Activity.Text.Contains("stop"))
                {
                    // Send End of conversation at the end.
                    var messageText = $"ending conversation from the skill...";
                    await turnContext.SendActivityAsync(MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput), cancellationToken);
                    var endOfConversation = Activity.CreateEndOfConversationActivity();
                    endOfConversation.Code = EndOfConversationCodes.CompletedSuccessfully;
                    await turnContext.SendActivityAsync(endOfConversation, cancellationToken);
                }
                else
                {
                    turnContext.Activity.RemoveRecipientMention();
                    string actualText = turnContext.Activity.Text;
                    if (!string.IsNullOrWhiteSpace(actualText))
                    {
                        actualText = actualText.Trim();
                        await CommandHandler.HandleCommand(turnContext, actualText, this, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("App sent a message with empty text"), cancellationToken).ConfigureAwait(false);
                if (turnContext.Activity.Value != null)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"but with value {JsonConvert.SerializeObject(turnContext.Activity.Value)}"), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        protected override Task OnEndOfConversationActivityAsync(ITurnContext<IEndOfConversationActivity> turnContext, CancellationToken cancellationToken)
        {
            // This will be called if the root bot is ending the conversation.  Sending additional messages should be
            // avoided as the conversation may have been deleted.
            // Perform cleanup of resources if needed.
            return Task.CompletedTask;
        }

        //private static async Task SendAdaptiveCardAsync(ITurnContext<IMessageActivity> turnContext, string cardType, CancellationToken cancellationToken)
        //{
        //    AdaptiveCard adaptiveCard = cardType switch
        //    {
        //        "botaction" => CommandHandler.MakeAdaptiveCard("botaction"),
        //        "taskmodule" => CardSampleHelper.CreateAdaptiveCard2(),
        //        "submit" => CardSampleHelper.CreateAdaptiveCard3(),

        //        _ => throw new ArgumentOutOfRangeException(nameof(cardNumber)),
        //    };
        //    var replyActivity = MessageFactory.Attachment(adaptiveCard.ToAttachment());
        //    await turnContext.SendActivityAsync(replyActivity, cancellationToken).ConfigureAwait(false);
        //}

       
    }
}
