// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotnetIntegrationBot
{
    public static class IntegrationCommandHandler
    {
        public static async Task HandleIntegrationCommand(ITurnContext<IMessageActivity> turnContext, string actualText, TeamsSkillBot bot, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(actualText))
            {
                throw new NullReferenceException(nameof(actualText));
            }

            if (turnContext == null)
            {
                throw new NullReferenceException(nameof(turnContext));
            }

            if (bot == null)
            {
                throw new NullReferenceException(nameof(bot));
            }

            switch (actualText.ToLowerInvariant())
            {
                case "proactivent":
                    await HandleProactiveNoneThreadedAsync(turnContext, bot.GetConfig(), cancellationToken).ConfigureAwait(false);
                    break;
                case "proactive":
                    await HandleProactiveThreadedAsync(turnContext, cancellationToken).ConfigureAwait(false);
                    break;
                case "delete":
                    await HandleDeleteActivitiesAsync(turnContext, cancellationToken).ConfigureAwait(false);
                    break;
                case "update":
                    await HandleUpdateActivitiesAsync(turnContext, bot.GetActivityIds(), cancellationToken).ConfigureAwait(false);
                    break;
                case "1":
                    await SendAdaptiveCardAsync(turnContext, 1, cancellationToken).ConfigureAwait(false);
                    break;
                case "2":
                    await SendAdaptiveCardAsync(turnContext, 2, cancellationToken).ConfigureAwait(false);
                    break;
                case "3":
                    await SendAdaptiveCardAsync(turnContext, 3, cancellationToken).ConfigureAwait(false);
                    break;
                case "hero":
                    await turnContext.SendActivityAsync(MessageFactory.Attachment(CardSampleHelper.CreateHeroCard().ToAttachment()), cancellationToken).ConfigureAwait(false);
                    break;
                case "thumbnail":
                    await turnContext.SendActivityAsync(MessageFactory.Attachment(CardSampleHelper.CreateThumbnailCard().ToAttachment()), cancellationToken).ConfigureAwait(false);
                    break;
                case "receipt":
                    await turnContext.SendActivityAsync(MessageFactory.Attachment(CardSampleHelper.CreateReceiptCard().ToAttachment()), cancellationToken).ConfigureAwait(false);
                    break;
                case "signin":
                    await turnContext.SendActivityAsync(MessageFactory.Attachment(CardSampleHelper.CreateSigninCard().ToAttachment()), cancellationToken).ConfigureAwait(false);
                    break;
                case "carousel":
                    // NOTE: if cards are NOT the same height in a carousel, Teams will instead display as AttachmentLayoutTypes.List
                    await turnContext.SendActivityAsync(
                        MessageFactory.Carousel(new[] { CardSampleHelper.CreateHeroCard().ToAttachment(), CardSampleHelper.CreateHeroCard().ToAttachment(), CardSampleHelper.CreateHeroCard().ToAttachment() }),
                        cancellationToken).ConfigureAwait(false);
                    break;
                case "list":
                    // NOTE: MessageFactory.Attachment with multiple attachments will default to AttachmentLayoutTypes.List
                    await turnContext.SendActivityAsync(
                        MessageFactory.Attachment(new[] { CardSampleHelper.CreateHeroCard().ToAttachment(), CardSampleHelper.CreateHeroCard().ToAttachment(), CardSampleHelper.CreateHeroCard().ToAttachment() }),
                        cancellationToken).ConfigureAwait(false);
                    break;
                case "o365":
                    await SendO365CardAttachmentAsync(turnContext, cancellationToken).ConfigureAwait(false);
                    break;
                case "file":
                    await SendFileCardAsync(turnContext, cancellationToken).ConfigureAwait(false);
                    break;
                case "show members":
                    await ShowMembersAsync(turnContext, bot, cancellationToken).ConfigureAwait(false);
                    break;
                case "show channels":
                    await ShowChannelsAsync(turnContext, bot, cancellationToken).ConfigureAwait(false);
                    break;
                case "show details":
                    await ShowDetailsAsync(turnContext, bot, cancellationToken).ConfigureAwait(false);
                    break;
                case "task module":
                    await turnContext.SendActivityAsync(MessageFactory.Attachment(CardSampleHelper.CreateTaskModuleHeroCard()), cancellationToken).ConfigureAwait(false);
                    break;
                case "mention":
                    await ShowMention(turnContext, cancellationToken).ConfigureAwait(false);
                    break;
                case "upload file":
                    await ShowUploadFile(turnContext, bot, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    await bot.SendMessageAndLogActivityIdAsync(turnContext, $"You said: {turnContext.Activity.Text}", cancellationToken).ConfigureAwait(false);
                    break;
            }
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

        private static async Task HandleProactiveNoneThreadedAsync(ITurnContext<IMessageActivity> turnContext, IConfiguration config, CancellationToken cancellationToken)
        {
            var teamsChannelId = turnContext.Activity.Conversation.Id;
            var serviceUrl = turnContext.Activity.ServiceUrl;
            var credentials = new MicrosoftAppCredentials(config["BotAppId"], config["BotAppPassword"]);
            ConversationReference conversationReference = null;
            var proactiveMessage = MessageFactory.Text($"This is a proactive message");
            proactiveMessage.Label = turnContext.Activity.Id;
            var conversationParameters = new ConversationParameters
            {
                IsGroup = false,
                Bot = turnContext.Activity.Recipient,
                Members = new ChannelAccount[] { turnContext.Activity.From },
                TenantId = turnContext.Activity.Conversation.TenantId,
            };

            await ((BotFrameworkAdapter)turnContext.Adapter).CreateConversationAsync(
                teamsChannelId,
                serviceUrl,
                credentials,
                conversationParameters,
                async (turnContext1, cancellationToken1) =>
                {
                    conversationReference = turnContext1.Activity.GetConversationReference();
                    await ((BotFrameworkAdapter)turnContext.Adapter).ContinueConversationAsync(
                        config["BotAppId"],
                        conversationReference,
                        async (turnContext2, cancellationToken2) =>
                        {
                            await turnContext2.SendActivityAsync(proactiveMessage, cancellationToken2).ConfigureAwait(false);
                        },
                        cancellationToken1).ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false);
        }

        private static async Task HandleProactiveThreadedAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var activity = MessageFactory.Text($"I will send two messages to this thread");
            await turnContext.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);

            for (var i = 0; i < 2; i++)
            {
                var replyToActivity = MessageFactory.Text($"This is message {i + 1}/2 that will be sent.");
                replyToActivity.ApplyConversationReference(activity.GetConversationReference());
                await turnContext.SendActivityAsync(replyToActivity, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task HandleDeleteActivitiesAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var activity = MessageFactory.Text("This message will be deleted in 5 seconds");
            activity.ReplyToId = turnContext.Activity.Id;
            var activityId = await turnContext.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);
            Thread.Sleep(5000);
            await turnContext.DeleteActivityAsync(activityId.Id, cancellationToken).ConfigureAwait(false);
        }

        private static async Task HandleUpdateActivitiesAsync(ITurnContext<IMessageActivity> turnContext, List<string> activityIds, CancellationToken cancellationToken)
        {
            foreach (var activityId in activityIds)
            {
                var newActivity = MessageFactory.Text(turnContext.Activity.Text);
                newActivity.Id = activityId;
                await turnContext.UpdateActivityAsync(newActivity, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task SendAdaptiveCardAsync(ITurnContext<IMessageActivity> turnContext, int cardNumber, CancellationToken cancellationToken)
        {
            AdaptiveCard adaptiveCard = cardNumber switch
            {
                1 => CardSampleHelper.CreateAdaptiveCard1(),
                2 => CardSampleHelper.CreateAdaptiveCard2(),
                3 => CardSampleHelper.CreateAdaptiveCard3(),
                _ => throw new ArgumentOutOfRangeException(nameof(cardNumber)),
            };
            var replyActivity = MessageFactory.Attachment(adaptiveCard.ToAttachment());
            await turnContext.SendActivityAsync(replyActivity, cancellationToken).ConfigureAwait(false);
        }

        private static async Task SendO365CardAttachmentAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var card = CardSampleHelper.CreateSampleO365ConnectorCard();
            var cardAttachment = new Attachment
            {
                Content = card,
                ContentType = O365ConnectorCard.ContentType,
            };

            await turnContext.SendActivityAsync(MessageFactory.Attachment(cardAttachment), cancellationToken).ConfigureAwait(false);
        }

        private static async Task SendFileCardAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var filename = Constants.TeamsLogoFileName;
            var filePath = Path.Combine("Files", filename);
            var fileSize = new FileInfo(filePath).Length;
            await SendFileCardAsync(turnContext, filename, fileSize, cancellationToken).ConfigureAwait(false);
        }

        private static async Task SendFileCardAsync(ITurnContext turnContext, string filename, long filesize, CancellationToken cancellationToken)
        {
            var consentContext = new Dictionary<string, string>
            {
                { "filename", filename },
            };

            var fileCard = new FileConsentCard
            {
                Description = "This is the file I want to send you",
                SizeInBytes = filesize,
                AcceptContext = consentContext,
                DeclineContext = consentContext,
            };

            var asAttachment = new Attachment
            {
                Content = fileCard,
                ContentType = FileConsentCard.ContentType,
                Name = filename,
            };

            var replyActivity = turnContext.Activity.CreateReply();
            replyActivity.Attachments = new List<Attachment>() { asAttachment };

            await turnContext.SendActivityAsync(replyActivity, cancellationToken).ConfigureAwait(false);
        }

        private static async Task ShowMembersAsync(ITurnContext<IMessageActivity> turnContext, TeamsSkillBot bot, CancellationToken cancellationToken)
        {
            await ShowMembersAsync(turnContext, await TeamsInfo.GetMembersAsync(turnContext, cancellationToken).ConfigureAwait(false), bot, cancellationToken).ConfigureAwait(false);
        }

        private static async Task ShowMembersAsync(ITurnContext<IMessageActivity> turnContext, IEnumerable<TeamsChannelAccount> teamsChannelAccounts, TeamsSkillBot bot, CancellationToken cancellationToken)
        {
            var replyActivity = MessageFactory.Text($"Total of {teamsChannelAccounts.Count()} members are currently in team");
            await turnContext.SendActivityAsync(replyActivity).ConfigureAwait(false);

            var messages = teamsChannelAccounts
                .Select(teamsChannelAccount => $"{teamsChannelAccount.AadObjectId} --> {teamsChannelAccount.Name} -->  {teamsChannelAccount.UserPrincipalName}");

            await SendInBatchesAsync(turnContext, messages, bot, cancellationToken).ConfigureAwait(false);
        }

        private static async Task SendInBatchesAsync(ITurnContext<IMessageActivity> turnContext, IEnumerable<string> messages, TeamsSkillBot bot, CancellationToken cancellationToken)
        {
            var batch = new List<string>();
            foreach (var msg in messages)
            {
                batch.Add(msg);

                if (batch.Count == 10)
                {
                    await bot.SendMessageAndLogActivityIdAsync(turnContext, string.Join("<br>", batch), cancellationToken).ConfigureAwait(false);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await bot.SendMessageAndLogActivityIdAsync(turnContext, string.Join("<br>", batch), cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task ShowChannelsAsync(ITurnContext<IMessageActivity> turnContext, TeamsSkillBot bot, CancellationToken cancellationToken)
        {
            var teamInfo = turnContext.Activity.TeamsGetTeamInfo();
            
            if (teamInfo == null)
            {
                await bot.SendMessageAndLogActivityIdAsync(turnContext, "This only works in the team scope", cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var teamId = teamInfo.Id;

                var channels = await TeamsInfo.GetTeamChannelsAsync(turnContext, teamId, cancellationToken).ConfigureAwait(false);

                var replyActivity = MessageFactory.Text($"Total of {channels.Count} channels are currently in team");

                await turnContext.SendActivityAsync(replyActivity).ConfigureAwait(false);

                var messages = channels.Select(channel => $"{channel.Id} --> {channel.Name}");

                await SendInBatchesAsync(turnContext, messages, bot, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task ShowDetailsAsync(ITurnContext<IMessageActivity> turnContext, TeamsSkillBot bot, CancellationToken cancellationToken)
        {
            var teamInfo = turnContext.Activity.TeamsGetTeamInfo();

            if (teamInfo == null)
            {
                await bot.SendMessageAndLogActivityIdAsync(turnContext, "This only works in the team scope", cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var teamId = teamInfo.Id;
                var teamDetails = await TeamsInfo.GetTeamDetailsAsync(turnContext, teamId, cancellationToken).ConfigureAwait(false);
                await bot.SendMessageAndLogActivityIdAsync(turnContext, $"The team name is {teamDetails.Name}. The team ID is {teamDetails.Id}. The ADD GroupID is {teamDetails.AadGroupId}.", cancellationToken).ConfigureAwait(false);
            }   
        }

        private static async Task ShowMention(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var mention = new Mention
            {
                Mentioned = turnContext.Activity.From,
                Text = $"<at>{XmlConvert.EncodeName(turnContext.Activity.From.Name)}</at>",
            };

            var replyActivity = MessageFactory.Text($"Hello {mention.Text}.");
            replyActivity.Entities = new List<Entity> { mention };
            await turnContext.SendActivityAsync(replyActivity, cancellationToken).ConfigureAwait(false);
        }

        private static async Task ShowUploadFile(ITurnContext<IMessageActivity> turnContext, TeamsSkillBot bot, CancellationToken cancellationToken)
        {
            bool messageWithFileDownloadInfo = turnContext.Activity.Attachments?[0].ContentType == FileDownloadInfo.ContentType;
            if (messageWithFileDownloadInfo)
            {
                var file = turnContext.Activity.Attachments[0];
                var fileDownload = JObject.FromObject(file.Content).ToObject<FileDownloadInfo>();

                string filePath = Path.Combine("Files", file.Name);

                using var client = bot.GetHttpClientFactory().CreateClient();
                var response = await client.GetAsync(new Uri(fileDownload.DownloadUrl)).ConfigureAwait(false);
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fileStream).ConfigureAwait(false);

                var reply = MessageFactory.Text($"<b>{file.Name}</b> received and saved.");
                reply.TextFormat = "xml";
                await turnContext.SendActivityAsync(reply, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                string filename = Constants.TeamsLogoFileName;
                string filePath = Path.Combine("Files", filename);
                long fileSize = new FileInfo(filePath).Length;
                await SendFileCardAsync(turnContext, filename, fileSize, cancellationToken).ConfigureAwait(false);
            }
        }

        private struct CardData
        {
            public string Title { get; set; }

            public string Subtitle { get; set; }

            public string Text { get; set; }
        }
    }
}
