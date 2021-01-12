// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.FileUpload
{
    public class FileUploadDialog : ComponentDialog
    {
        public FileUploadDialog()
            : base(nameof(FileUploadDialog))
        {
            AddDialog(new AttachmentPrompt("AttachmentPrompt"));
            AddDialog(new ChoicePrompt("ChoicePrompt"));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[] { PromptUploadStepAsync, HandleAttachmentStepAsync, FinalStepAsync }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PromptUploadStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Please upload a file to continue."),
                RetryPrompt = MessageFactory.Text("You must upload a file."),
            };

            return await stepContext.PromptAsync("AttachmentPrompt", promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> HandleAttachmentStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var filetext = string.Empty;

            foreach (var file in stepContext.Context.Activity.Attachments)
            {
                var remoteFileUrl = file.ContentUrl;

                var localFileName = Path.Combine(Path.GetTempPath(), file.Name);

                using (var webClient = new WebClient())
                {
                    webClient.DownloadFile(remoteFileUrl, localFileName);
                }

                filetext += $"Attachment \"{file.Name}\"" +
                             $" has been received and saved to \"{localFileName}\"\r\n";
            }

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(filetext), cancellationToken);

            var messageText = "Do you want to upload another file?";
            var repromptMessageText = "You must select \"Yes\" or \"No\".";
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
                Choices = new List<Choice> { new Choice("Yes"), new Choice("No") },
                Style = ListStyle.List
            };

            return await stepContext.PromptAsync("ChoicePrompt", options, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var choice = ((FoundChoice)stepContext.Result).Value.ToLowerInvariant();

            if (choice == "yes")
            {
                return await stepContext.ReplaceDialogAsync(InitialDialogId, cancellationToken: cancellationToken);
            }
            else
            {
                return new DialogTurnResult(DialogTurnStatus.Complete);
            }
        }
    }
}
