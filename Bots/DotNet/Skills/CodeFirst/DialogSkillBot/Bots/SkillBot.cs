// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Bots
{
    public class SkillBot<T> : ActivityHandler
        where T : Dialog
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ConcurrentDictionary<string, ConversationReference> _conversationReferences;
        private readonly ConversationState _conversationState;
        private readonly Dialog _mainDialog;

        public SkillBot(ConversationState conversationState, T mainDialog, IHttpClientFactory clientFactory, ConcurrentDictionary<string, ConversationReference> conversationReferences)
        {
            _conversationState = conversationState;
            _mainDialog = mainDialog;
            _clientFactory = clientFactory;
            _conversationReferences = conversationReferences;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            AddConversationReference(turnContext.Activity);

            if (turnContext.Activity.Type != ActivityTypes.ConversationUpdate)
            {
                // Run the Dialog with the Activity.
                await _mainDialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
            }
            else
            {
                // Let the base class handle the activity.
                await base.OnTurnAsync(turnContext, cancellationToken);
            }

            // Save any state changes that might have occurred during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            AddConversationReference(turnContext.Activity as Activity);

            await base.OnMessageActivityAsync(turnContext, cancellationToken);
        }

        protected override Task OnConversationUpdateActivityAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            AddConversationReference(turnContext.Activity as Activity);

            return base.OnConversationUpdateActivityAsync(turnContext, cancellationToken);
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

        private void AddConversationReference(Activity activity)
        {
            var conversationReference = activity.GetConversationReference();
            _conversationReferences.AddOrUpdate(conversationReference.User.Id, conversationReference, (key, newValue) => conversationReference);
        }
    }
}
