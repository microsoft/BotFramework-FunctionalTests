// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.BotFrameworkFunctionalTests.TeamsWaterfallSkillBot.Dialogs.Cards;
using Microsoft.BotFrameworkFunctionalTests.TeamsWaterfallSkillBot.Dialogs.Proactive;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotFrameworkFunctionalTests.TeamsWaterfallSkillBot.Bots
{
    public class SkillBot<T> : TeamsActivityHandler
        where T : Dialog
    {
        private readonly ConcurrentDictionary<string, ContinuationParameters> _continuationParameters;
        private readonly ConversationState _conversationState;
        private readonly Dialog _mainDialog;
        private readonly Uri _serverUrl;

        public SkillBot(ConversationState conversationState, T mainDialog, ConcurrentDictionary<string, ContinuationParameters> continuationParameters, IHttpContextAccessor httpContextAccessor)
        {
            _conversationState = conversationState;
            _mainDialog = mainDialog;
            _continuationParameters = continuationParameters;
            _serverUrl = new Uri($"{httpContextAccessor.HttpContext.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host.Value}");
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

        internal static Task<MessagingExtensionActionResponse> CreateCardCommand(MessagingExtensionAction action)
        {
            if (action == null)
            {
                throw new NullReferenceException(nameof(action));
            }

            // The user has chosen to create a card by choosing the 'Create Card' context menu command.
            var createCardData = ((JObject)action.Data).ToObject<CardData>();

            var card = new HeroCard
            {
                Title = createCardData.Title,
                Subtitle = createCardData.Subtitle,
                Text = createCardData.Text,
            };

            var attachments = new List<MessagingExtensionAttachment>
            {
                new MessagingExtensionAttachment
                {
                    Content = card,
                    ContentType = HeroCard.ContentType,
                    Preview = card.ToAttachment(),
                }
            };

            return Task.FromResult(new MessagingExtensionActionResponse
            {
                ComposeExtension = new MessagingExtensionResult
                {
                    AttachmentLayout = "list",
                    Type = "result",
                    Attachments = attachments,
                },
            });
        }

        internal static Task<MessagingExtensionActionResponse> ShareMessageCommand(MessagingExtensionAction action)
        {
            // The user has chosen to share a message by choosing the 'Share Message' context menu command.
            var heroCard = new HeroCard
            {
                Title = $"{action.MessagePayload.From?.User?.DisplayName} orignally sent this message:",
                Text = action.MessagePayload.Body.Content,
            };

            if (action.MessagePayload.Attachments?.Count > 0)
            {
                // This sample does not add the MessagePayload Attachments.  This is left as an
                // exercise for the user.
                heroCard.Subtitle = $"({action.MessagePayload.Attachments.Count} Attachments not included)";
            }

            // This Messaging Extension example allows the user to check a box to include an image with the
            // shared message.  This demonstrates sending custom parameters along with the message payload.
            var includeImage = ((JObject)action.Data)["includeImage"]?.ToString();
            if (!string.IsNullOrEmpty(includeImage) && includeImage == bool.TrueString)
            {
                heroCard.Images = new List<CardImage>
                {
                    new CardImage { Url = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQtB3AwMUeNoq4gUBGe6Ocj8kyh3bXa9ZbV7u1fVKQoyKFHdkqU" },
                };
            }

            return Task.FromResult(new MessagingExtensionActionResponse
            {
                ComposeExtension = new MessagingExtensionResult
                {
                    Type = "result",
                    AttachmentLayout = "list",
                    Attachments = new List<MessagingExtensionAttachment>()
                    {
                        new MessagingExtensionAttachment
                        {
                            Content = heroCard,
                            ContentType = HeroCard.ContentType,
                            Preview = heroCard.ToAttachment(),
                        },
                    }
                },
            });
        }

        internal static Task<MessagingExtensionActionResponse> CreateMessagePreview(MessagingExtensionAction action)
        {
            var sampleData = JsonConvert.DeserializeObject<SampleData>(action.Data.ToString());
            var adaptiveCard = CardSampleHelper.CreateAdaptiveCard(sampleData);
            return Task.FromResult(new MessagingExtensionActionResponse
            {
                ComposeExtension = new MessagingExtensionResult
                {
                    Type = "botMessagePreview",
                    ActivityPreview = MessageFactory.Attachment(new Attachment
                    {
                        Content = adaptiveCard,
                        ContentType = AdaptiveCard.ContentType,
                    }) as Activity,
                },
            });
        }

        protected override Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionSubmitActionAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return action.CommandId switch
            {
                "createCard" => CreateCardCommand(action),
                "shareMessage" => ShareMessageCommand(action),
                "createWithPreview" => CreateMessagePreview(action),
#pragma warning disable RCS1079 // Throwing of new NotImplementedException.
                _ => throw new NotImplementedException($"Invalid CommandId: {action.CommandId}"),
#pragma warning restore RCS1079 // Throwing of new NotImplementedException.
            };
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    var activity = MessageFactory.Text("Welcome to the waterfall skill bot. \n\nThis is a skill, you will need to call it from another bot to use it.");
                    activity.Speak = "Welcome to the waterfall skill bot. This is a skill, you will need to call it from another bot to use it.";
                    await turnContext.SendActivityAsync(activity, cancellationToken);

                    await turnContext.SendActivityAsync($"You can check the skill manifest to see what it supports here: {_serverUrl}manifests/waterfallskillbot-manifest-1.0.json", cancellationToken: cancellationToken);
                }
            }
        }

        protected override async Task OnTeamsSigninVerifyStateAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            // The OAuth Prompt needs to see the Invoke Activity in order to complete the login process.

            // Run the Dialog with the new Invoke Activity.
            await _mainDialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
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

        private struct CardData
        {
            public string Title { get; set; }

            public string Subtitle { get; set; }

            public string Text { get; set; }
        }
    }
}
