// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Proactive;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Bots
{
    public class SkillBot<T> : ActivityHandler
        where T : Dialog
    {
        private readonly ConcurrentDictionary<string, ContinuationParameters> _continuationParameters;
        private readonly ConversationState _conversationState;
        private readonly Dialog _mainDialog;

        public SkillBot(ConversationState conversationState, T mainDialog, ConcurrentDictionary<string, ContinuationParameters> continuationParameters)
        {
            _conversationState = conversationState;
            _mainDialog = mainDialog;
            _continuationParameters = continuationParameters;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            // Store continuation parameters for proactive messages.
            AddOrUpdateContinuationParameters(turnContext);

            if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                // Let the base class handle the activity (this will trigger OnMembersAddedAsync).
                await base.OnTurnAsync(turnContext, cancellationToken);
            }
            else
            {
                // Run the Dialog with the Activity.
                await _mainDialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
            }

            // Save any state changes that might have occurred during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                // Greet anyone that was not the target (recipient) of this message.
                // To learn more about Adaptive Cards, see https://aka.ms/msbot-adaptivecards.
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    var activity = MessageFactory.Text("Welcome to the dialog skill bot");
                    activity.Speak = "Welcome to the Dialog Skill Prototype!";
                    await turnContext.SendActivityAsync(activity, cancellationToken);
                    await _mainDialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
                }
            }
        }

        /// <summary>
        /// Helper to extract and store parameters we need to continue a conversation from a proactive message.
        /// </summary>
        /// <param name="turnContext">A turnContext instance with the parameters we need.</param>
        private void AddOrUpdateContinuationParameters(ITurnContext turnContext)
        {
            var continuationParameters = new ContinuationParameters
            {
                ClaimsIdentity = turnContext.TurnState.Get<IIdentity>(BotAdapter.BotIdentityKey),
                ConversationReference = turnContext.Activity.GetConversationReference(),
                OAuthScope = turnContext.TurnState.Get<string>(BotAdapter.OAuthScopeKey)
            };

            _continuationParameters.AddOrUpdate(continuationParameters.ConversationReference.User.Id, continuationParameters, (key, newValue) => continuationParameters);
        }
    }
}
