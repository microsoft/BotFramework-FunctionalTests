﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.BotFrameworkFunctionalTests.DialogSkillBot.Cards;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotFrameworkFunctionalTests.DialogSkillBot.Dialogs
{
    public class CardDialog : ComponentDialog
    {
        // for file upload
        private static readonly string TeamsLogoFileName = "teams-logo.png";

        // for audio card
        private static readonly string BellSound = "music.mp3";

        // for video card
        private static readonly string CorgiOnCarouselVideo = "https://www.youtube.com/watch?v=LvqzubPZjHE";

        // for animation card
        private static readonly string MindBlownGif = "https://media3.giphy.com/media/xT0xeJpnrWC4XWblEk/giphy.gif?cid=ecf05e47mye7k75sup6tcmadoom8p1q8u03a7g2p3f76upp9&rid=giphy.gif";

        // list of cards that exist
        private static readonly List<string> _cardOptions = new List<string>
        {
            "botaction",
            "taskmodule",
            "submitaction",
            "hero",
            "thumbnail",
            "receipt",
            "signin",
            "carousel",
            "list",
            "o365",
            "file",
            "animation",
            "audio",
            "video",
            "uploadfile"
        };

        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _clientFactory;

        public CardDialog(IConfiguration config, IHttpClientFactory clientFactory)
            : base(nameof(CardDialog))
        {
            _config = config;
            _clientFactory = clientFactory;

            AddDialog(new ChoicePrompt("CardPrompt", SkillActionPromptValidator));

            AddDialog(new ChoicePrompt("EndPrompt", SkillActionPromptValidator));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[] { SelectCardAsync, DisplayCardAsync, FinalStepAsync }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        public IHttpClientFactory GetClientFactory()
        {
            return _clientFactory;
        }

        private async Task<DialogTurnResult> SelectCardAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Create the PromptOptions from the skill configuration which contain the list of configured skills.
            var messageText = "What card do you want?";
            var repromptMessageText = "That was not a valid choice, please select a valid card type.";
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
                Choices = _cardOptions.Select(card => new Choice(card)).ToList()
            };

            // Ask the user to enter their name.
            return await stepContext.PromptAsync("CardPrompt", options, cancellationToken);
        }

        private async Task<DialogTurnResult> DisplayCardAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var card = stepContext.Context.Activity.Text;

            switch (card)
            {
                case "botaction":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(MakeAdaptiveCard("botaction").ToAttachment()));
                    break;
                case "taskmodule":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(MakeAdaptiveCard("taskmodule").ToAttachment()));
                    break;
                case "submitaction":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(MakeAdaptiveCard("submitaction").ToAttachment()));
                    break;
                case "hero":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(CardSampleHelper.CreateHeroCard().ToAttachment()), cancellationToken).ConfigureAwait(false);
                    break;
                case "thumbnail":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(CardSampleHelper.CreateThumbnailCard().ToAttachment()), cancellationToken).ConfigureAwait(false);
                    break;
                case "receipt":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(CardSampleHelper.CreateReceiptCard().ToAttachment()), cancellationToken).ConfigureAwait(false);
                    break;
                case "signin":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(CardSampleHelper.CreateSigninCard().ToAttachment()), cancellationToken).ConfigureAwait(false);
                    break;
                case "carousel":
                    // NOTE: if cards are NOT the same height in a carousel, Teams will instead display as AttachmentLayoutTypes.List
                    await stepContext.Context.SendActivityAsync(
                        MessageFactory.Carousel(new[]
                        {
                                    CardSampleHelper.CreateHeroCard().ToAttachment(),
                                    CardSampleHelper.CreateHeroCard().ToAttachment(),
                                    CardSampleHelper.CreateHeroCard().ToAttachment()
                        }),
                        cancellationToken).ConfigureAwait(false);
                    break;
                case "list":
                    // NOTE: MessageFactory.Attachment with multiple attachments will default to AttachmentLayoutTypes.List
                    await stepContext.Context.SendActivityAsync(
                        MessageFactory.Attachment(new[]
                        {
                                    CardSampleHelper.CreateHeroCard().ToAttachment(),
                                    CardSampleHelper.CreateHeroCard().ToAttachment(),
                                    CardSampleHelper.CreateHeroCard().ToAttachment()
                        }),
                        cancellationToken).ConfigureAwait(false);
                    break;
                case "o365":

                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(MakeO365CardAttachmentAsync()), cancellationToken).ConfigureAwait(false);
                    break;
                case "file":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(MakeFileCard()));
                    break;
                case "animation":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(MakeAnimationCard().ToAttachment()));
                    break;
                case "audio":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(MakeAudiocard().ToAttachment()));
                    break;
                case "video":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(MakeVideoCard().ToAttachment()));
                    break;
                case "uploadfile":
                    await ShowUploadFile(stepContext, cancellationToken).ConfigureAwait(false);
                    break;
            }

            var messageText = "Do you want to select another card?";
            var repromptMessageText = "That was not a valid choice. Do you want another card? Send \"Yes\" or \"No\".";
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
                Choices = new List<Choice> { new Choice("Yes"), new Choice("No") }
            };

            return await stepContext.PromptAsync("EndPrompt", options, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var selectedSkillId = ((FoundChoice)stepContext.Result).Value.ToLower();

            if (selectedSkillId == "yes")
            {
                return await stepContext.ReplaceDialogAsync(InitialDialogId, "What card would you want?", cancellationToken);
            }

            return new DialogTurnResult(DialogTurnStatus.Complete);
        }

        private async Task<bool> SkillActionPromptValidator(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            if (!promptContext.Recognized.Succeeded)
            {
                await promptContext.Context.SendActivityAsync(MessageFactory.Text("App sent a message with empty text"), cancellationToken).ConfigureAwait(false);
                if (promptContext.Context.Activity.Value != null)
                {
                    await promptContext.Context.SendActivityAsync(MessageFactory.Text($"but with value {JsonConvert.SerializeObject(promptContext.Context.Activity.Value)}"), cancellationToken).ConfigureAwait(false);
                }

                await promptContext.Context.SendActivityAsync(promptContext.Options.RetryPrompt);

                return await Task.FromResult(false);
            }
           
            return await Task.FromResult(true);   
        }

        private AdaptiveCard MakeAdaptiveCard(string cardType)
        {
            var adaptiveCard = cardType switch
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
            var filename = TeamsLogoFileName;
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
            var url = new MediaUrl(url: MindBlownGif);
            return new AnimationCard(title: "Animation Card", media: new[] { url }, autostart: true);
        }

        private VideoCard MakeVideoCard()
        {
            var url = new MediaUrl(url: CorgiOnCarouselVideo);
            return new VideoCard(title: "Video Card", media: new[] { url });
        }

        private AudioCard MakeAudiocard()
        {
            var url = new MediaUrl(url: _config["AudioUrl"]);
            return new AudioCard(title: "Audio Card", media: new[] { url }, autoloop: true);
        }

        private async Task ShowUploadFile(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var messageWithFileDownloadInfo = stepContext.Context.Activity.Attachments?[0].ContentType == FileDownloadInfo.ContentType;
            if (messageWithFileDownloadInfo)
            {
                var file = stepContext.Context.Activity.Attachments[0];
                var fileDownload = JObject.FromObject(file.Content).ToObject<FileDownloadInfo>();

                var filePath = Path.Combine("Files", file.Name);

                using var client = GetClientFactory().CreateClient();
                var response = await client.GetAsync(new Uri(fileDownload.DownloadUrl)).ConfigureAwait(false);
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fileStream).ConfigureAwait(false);

                var reply = MessageFactory.Text($"<b>{file.Name}</b> received and saved.");
                reply.TextFormat = "xml";
                await stepContext.Context.SendActivityAsync(reply, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var filename = TeamsLogoFileName;
                var filePath = Path.Combine("Files", filename);
                var fileSize = new FileInfo(filePath).Length;
                await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(MakeFileCardAttachment(filename, fileSize))).ConfigureAwait(false);
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
