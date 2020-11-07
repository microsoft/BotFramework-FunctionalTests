// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Security.Policy;
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

namespace CardSkill
{
    public static class CommandHandler
    {
        public static readonly string CorgiOnCarouselVideo = "https://www.youtube.com/watch?v=LvqzubPZjHE";
        public static readonly string MindBlownGif = "https://media3.giphy.com/media/xT0xeJpnrWC4XWblEk/giphy.gif?cid=ecf05e47mye7k75sup6tcmadoom8p1q8u03a7g2p3f76upp9&rid=giphy.gif";

        private static IEnumerable<Attachment> _listOfAllCards = new[]
        {
            MakeAdaptiveCard("botaction").ToAttachment(),
            MakeAdaptiveCard("taskmodule").ToAttachment(),
            MakeAdaptiveCard("submitaction").ToAttachment(),
            CardSampleHelper.CreateHeroCard().ToAttachment(),
            CardSampleHelper.CreateThumbnailCard().ToAttachment(),
            CardSampleHelper.CreateReceiptCard().ToAttachment(),
            CardSampleHelper.CreateSigninCard().ToAttachment(),
            MakeO365CardAttachmentAsync()
        };

        public static IEnumerable<Attachment> GetListOfAllCards()
        {
            return _listOfAllCards;
        }

        public static async Task HandleCommand(ITurnContext<IMessageActivity> turnContext, string actualText, CardBot bot, CancellationToken cancellationToken)
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
                case "botaction":
                    await turnContext.SendActivityAsync(MessageFactory.Attachment(MakeAdaptiveCard("botaction").ToAttachment()));
                    break;
                case "taskmodule":
                    await turnContext.SendActivityAsync(MessageFactory.Attachment(MakeAdaptiveCard("taskmodule").ToAttachment()));
                    break;
                case "submitaction":
                    await turnContext.SendActivityAsync(MessageFactory.Attachment(MakeAdaptiveCard("submitaction").ToAttachment()));
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

                    await turnContext.SendActivityAsync(MessageFactory.Attachment(MakeO365CardAttachmentAsync()), cancellationToken).ConfigureAwait(false);
                    break;
                case "file":
                    await turnContext.SendActivityAsync(MessageFactory.Attachment(MakeFileCard()));
                    break;
                case "animation":
                    await turnContext.SendActivityAsync(MessageFactory.Attachment(MakeAnimationCard().ToAttachment()));
                    break;
                case "audio":
                    await turnContext.SendActivityAsync(MessageFactory.Attachment(MakeAudiocard().ToAttachment()));
                    break;
                case "video":
                    await turnContext.SendActivityAsync(MessageFactory.Attachment(MakeVideoCard().ToAttachment()));
                    break;
                case "uploadfile":
                    await ShowUploadFile(turnContext, bot, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    await turnContext.SendActivityAsync(MessageFactory.Text($"You said: {turnContext.Activity.Text}"), cancellationToken).ConfigureAwait(false);
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

        private static AnimationCard MakeAnimationCard()
        {
            MediaUrl url = new MediaUrl(url: MindBlownGif);
            return new AnimationCard(title: "Animation Card", media: new[] { url }, autostart: true);
        }

        private static VideoCard MakeVideoCard()
        {
            MediaUrl url = new MediaUrl(url: CorgiOnCarouselVideo);
            return new VideoCard(title: "Video Card", media: new[] { url });
        }

        private static AudioCard MakeAudiocard()
        {
            MediaUrl url = new MediaUrl(url: "https://skillsbot.azurewebsites.net/api/bell");
            return new AudioCard(title: "Audio Card", media: new[] { url }, autoloop: true);
        }

        private static AdaptiveCard MakeAdaptiveCard(string cardType)
        {
            AdaptiveCard adaptiveCard = cardType switch
            {
                "botaction" => CardSampleHelper.CreateAdaptiveCardBotAction(),
                "taskmodule" => CardSampleHelper.CreateAdaptiveCardTaskModule(),
                "submitaction" => CardSampleHelper.CreateAdaptiveCardSubmit(),
                _ => throw new ArgumentException(nameof(cardType)),
            };

            return adaptiveCard;
        }

        private static Attachment MakeO365CardAttachmentAsync()
        {
            var card = CardSampleHelper.CreateSampleO365ConnectorCard();
            var cardAttachment = new Attachment
            {
                Content = card,
                ContentType = O365ConnectorCard.ContentType,
            };

            return cardAttachment;
        }

        private static Attachment MakeFileCard()
        {
            var filename = Constants.TeamsLogoFileName;
            var filePath = Path.Combine("Files", filename);
            var fileSize = new FileInfo(filePath).Length;

            return MakeFileCardAttachment(filename, fileSize);
        }

        private static Attachment MakeFileCardAttachment(string filename, long filesize)
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

            return asAttachment;
        }

        private static async Task ShowUploadFile(ITurnContext<IMessageActivity> turnContext, CardBot bot, CancellationToken cancellationToken)
        {
            bool messageWithFileDownloadInfo = turnContext.Activity.Attachments?[0].ContentType == FileDownloadInfo.ContentType;
            if (messageWithFileDownloadInfo)
            {
                var file = turnContext.Activity.Attachments[0];
                var fileDownload = JObject.FromObject(file.Content).ToObject<FileDownloadInfo>();

                string filePath = Path.Combine("Files", file.Name);

                using var client = bot.GetClientFactory().CreateClient();
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
                await turnContext.SendActivityAsync(MessageFactory.Attachment(MakeFileCardAttachment(filename, fileSize))).ConfigureAwait(false);
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
