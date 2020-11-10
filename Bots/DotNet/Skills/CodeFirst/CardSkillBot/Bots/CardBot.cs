// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CardSkill
{
    public class CardBot : ActivityHandler
    {
        private static readonly string _corgiOnCarouselVideo = "https://www.youtube.com/watch?v=LvqzubPZjHE";
        private static readonly string _mindBlownGif = "https://media3.giphy.com/media/xT0xeJpnrWC4XWblEk/giphy.gif?cid=ecf05e47mye7k75sup6tcmadoom8p1q8u03a7g2p3f76upp9&rid=giphy.gif";
        private readonly string _possibleCards = "botaction, taskmodule, submit, hero, thumbnail, receipt, signin, carousel, list, o365, file, uploadfile, animation, video, audio";
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _clientFactory;

        public CardBot(IHttpClientFactory clientFactory, IConfiguration config)
        {
            _clientFactory = clientFactory;
            _config = config;
        }

        public IHttpClientFactory GetClientFactory()
        {
            return _clientFactory;
        }

        protected override async Task OnConversationUpdateActivityAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(MessageFactory.Text($"You can send me the following messages to see all cards: {_possibleCards}"));
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
                    switch (turnContext.Activity.Text.ToLowerInvariant())
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
                            await ShowUploadFile(turnContext, cancellationToken).ConfigureAwait(false);
                            break;
                        default:
                            await turnContext.SendActivityAsync(MessageFactory.Text($"You said: {turnContext.Activity.Text}"), cancellationToken).ConfigureAwait(false);
                            break;
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

        private Task<MessagingExtensionActionResponse> CreateCardCommand(MessagingExtensionAction action)
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

        private Task<MessagingExtensionActionResponse> ShareMessageCommand(MessagingExtensionAction action)
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

        private Task<MessagingExtensionActionResponse> CreateMessagePreview(MessagingExtensionAction action)
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

        private AdaptiveCard MakeAdaptiveCard(string cardType)
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

        private Attachment MakeO365CardAttachmentAsync()
        {
            var card = CardSampleHelper.CreateSampleO365ConnectorCard();
            var cardAttachment = new Attachment
            {
                Content = card,
                ContentType = O365ConnectorCard.ContentType,
            };

            return cardAttachment;
        }

        private Attachment MakeFileCard()
        {
            var filename = Constants.TeamsLogoFileName;
            var filePath = Path.Combine("Files", filename);
            var fileSize = new FileInfo(filePath).Length;

            return MakeFileCardAttachment(filename, fileSize);
        }

        private Attachment MakeFileCardAttachment(string filename, long filesize)
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

        private AnimationCard MakeAnimationCard()
        {
            MediaUrl url = new MediaUrl(url: _mindBlownGif);
            return new AnimationCard(title: "Animation Card", media: new[] { url }, autostart: true);
        }

        private VideoCard MakeVideoCard()
        {
            MediaUrl url = new MediaUrl(url: _corgiOnCarouselVideo);
            return new VideoCard(title: "Video Card", media: new[] { url });
        }

        private AudioCard MakeAudiocard()
        {
            MediaUrl url = new MediaUrl(url: _config["AudioUrl"]);
            return new AudioCard(title: "Audio Card", media: new[] { url }, autoloop: true);
        }

        private async Task ShowUploadFile(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            bool messageWithFileDownloadInfo = turnContext.Activity.Attachments?[0].ContentType == FileDownloadInfo.ContentType;
            if (messageWithFileDownloadInfo)
            {
                var file = turnContext.Activity.Attachments[0];
                var fileDownload = JObject.FromObject(file.Content).ToObject<FileDownloadInfo>();

                string filePath = Path.Combine("Files", file.Name);

                using var client = GetClientFactory().CreateClient();
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
