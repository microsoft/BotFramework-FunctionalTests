﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallHostBot.Bots
{
    public class RootBot<T> : ActivityHandler
        where T : Dialog
    {
        public const string ActiveSkillPropertyName = "activeSkillProperty";

        private readonly IStatePropertyAccessor<BotFrameworkSkill> _activeSkillProperty;
        private readonly ConversationState _conversationState;
        private readonly Dialog _mainDialog;

        public RootBot(ConversationState conversationState, T mainDialog, UserState userState)
        {
            _conversationState = conversationState;
            _mainDialog = mainDialog;

            // Create state property to track the active skill
            _activeSkillProperty = _conversationState.CreateProperty<BotFrameworkSkill>(ActiveSkillPropertyName);
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                // Let the base class handle the activity (this will trigger OnMembersAdded).
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
                    var welcomeCard = CreateAdaptiveCardAttachment();
                    var activity = MessageFactory.Attachment(welcomeCard);
                    activity.Speak = "Welcome to the waterfall host bot";
                    await turnContext.SendActivityAsync(activity, cancellationToken);
                    await _mainDialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
                }
            }
        }

        protected override async Task OnEndOfConversationActivityAsync(ITurnContext<IEndOfConversationActivity> turnContext, CancellationToken cancellationToken)
        {
            // forget skill invocation
            await _activeSkillProperty.DeleteAsync(turnContext, cancellationToken);

            await _conversationState.LoadAsync(turnContext, true, cancellationToken);
            await _mainDialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
        }

        // Load attachment from embedded resource.
        private Attachment CreateAdaptiveCardAttachment()
        {
            var cardResourcePath = "Microsoft.BotFrameworkFunctionalTests.WaterfallHostBot.Cards.welcomeCard.json";

            using (var stream = GetType().Assembly.GetManifestResourceStream(cardResourcePath))
            {
                using (var reader = new StreamReader(stream))
                {
                    var adaptiveCard = reader.ReadToEnd();
                    return new Attachment
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(adaptiveCard)
                    };
                }
            }
        }
    }
}
