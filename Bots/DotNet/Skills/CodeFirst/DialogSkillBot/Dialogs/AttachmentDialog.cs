using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;

namespace Microsoft.BotFrameworkFunctionalTests.DialogSkillBot.Dialogs
{
    public class AttachmentDialog : ComponentDialog
    {
        public AttachmentDialog()
             : base(nameof(AttachmentDialog))
        {
            AddDialog(new ChoicePrompt("AttachmentTypePrompt"));

            AddDialog(new ChoicePrompt("EndPrompt"));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[] { SelectAttachmentAsync, HandleAttachmentAsync, FinalStepAsync }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private static Attachment GetInlineAttachment()
        {
            var imagePath = Path.Combine(Environment.CurrentDirectory, @"Files", "architecture-resize.png");
            var imageData = Convert.ToBase64String(File.ReadAllBytes(imagePath));

            return new Attachment
            {
                Name = @"Resources\architecture-resize.png",
                ContentType = "image/png",
                ContentUrl = $"data:image/png;base64,{imageData}",
            };
        }

        private static Attachment GetInternetAttachment()
        {
            // ContentUrl must be HTTPS.
            return new Attachment
            {
                Name = @"Resources\architecture-resize.png",
                ContentType = "image/png",
                ContentUrl = "https://docs.microsoft.com/en-us/bot-framework/media/how-it-works/architecture-resize.png",
            };
        }

        private static async Task<Attachment> GetUploadedAttachmentAsync(WaterfallStepContext stepContext, string serviceUrl, string conversationId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(serviceUrl))
            {
                throw new ArgumentNullException(nameof(serviceUrl));
            }

            if (string.IsNullOrWhiteSpace(conversationId))
            {
                throw new ArgumentNullException(nameof(conversationId));
            }

            var imagePath = Path.Combine(Environment.CurrentDirectory, @"Files", "architecture-resize.png");

            var connector = stepContext.Context.TurnState.Get<IConnectorClient>() as ConnectorClient;
            var attachments = new Attachments(connector);
            var response = await attachments.Client.Conversations.UploadAttachmentAsync(
                conversationId,
                new AttachmentData
                {
                    Name = @"Resources\architecture-resize.png",
                    OriginalBase64 = File.ReadAllBytes(imagePath),
                    Type = "image/png",
                },
                cancellationToken);

            var attachmentUri = attachments.GetAttachmentUri(response.Id);

            return new Attachment
            {
                Name = @"Resources\architecture-resize.png",
                ContentType = "image/png",
                ContentUrl = attachmentUri,
            };
        }

        private async Task<DialogTurnResult> SelectAttachmentAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Create the PromptOptions from the skill configuration which contain the list of configured skills.
            var messageText = "What card do you want?";
            var repromptMessageText = "That was not a valid choice, please select a valid card type.";
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
                Choices = new List<Choice> { new Choice("Inline"), new Choice("Internet"), new Choice("Upload") } 
            };

            // Ask the user to enter their name.
            return await stepContext.PromptAsync("AttachmentTypePrompt", options, cancellationToken);
        }

        private async Task<DialogTurnResult> HandleAttachmentAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var card = stepContext.Context.Activity.Text;
            var reply = MessageFactory.Text(string.Empty);

            switch (card)
            {
                case "Inline":
                    reply.Text = "This is an inline attachment.";
                    reply.Attachments = new List<Attachment>() { GetInlineAttachment() };
                    break;

                case "Internet":
                    reply.Text = "This is an attachment from a HTTP URL.";
                    reply.Attachments = new List<Attachment>() { GetInternetAttachment() };
                    break;

                case "Upload":
                    reply.Text = "This is an uploaded attachment.";

                    // Get the uploaded attachment.
                    var uploadedAttachment = await GetUploadedAttachmentAsync(stepContext, stepContext.Context.Activity.ServiceUrl, stepContext.Context.Activity.Conversation.Id, cancellationToken);
                    reply.Attachments = new List<Attachment>() { uploadedAttachment };
                    break;

                default:
                    reply.Text = "Invalid choice";
                    break;
            }

            await stepContext.Context.SendActivityAsync(reply, cancellationToken);

            var messageText = "Do you want another type of attachment?";
            var repromptMessageText = "That's an invalid choice.";

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
                return await stepContext.ReplaceDialogAsync(InitialDialogId, "What attachment type do you want?", cancellationToken);
            }

            return new DialogTurnResult(DialogTurnStatus.Complete);
        }
    }
}
