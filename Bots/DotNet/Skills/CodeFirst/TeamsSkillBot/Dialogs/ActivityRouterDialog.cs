// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Schema;
using Microsoft.BotFrameworkFunctionalTests.TeamsSkillBot.Dialogs.Attachments;
using Microsoft.BotFrameworkFunctionalTests.TeamsSkillBot.Dialogs.Auth;
using Microsoft.BotFrameworkFunctionalTests.TeamsSkillBot.Dialogs.Cards;
using Microsoft.BotFrameworkFunctionalTests.TeamsSkillBot.Dialogs.Delete;
using Microsoft.BotFrameworkFunctionalTests.TeamsSkillBot.Dialogs.Echo;
using Microsoft.BotFrameworkFunctionalTests.TeamsSkillBot.Dialogs.FileUpload;
using Microsoft.BotFrameworkFunctionalTests.TeamsSkillBot.Dialogs.Proactive;
using Microsoft.BotFrameworkFunctionalTests.TeamsSkillBot.Dialogs.Sso;
using Microsoft.BotFrameworkFunctionalTests.TeamsSkillBot.Dialogs.Update;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Microsoft.BotFrameworkFunctionalTests.TeamsSkillBot.Dialogs
{
    /// <summary>
    /// A root dialog that can route activities sent to the skill to different sub-dialogs.
    /// </summary>
    public class ActivityRouterDialog : ComponentDialog
    {
        public ActivityRouterDialog(IConfiguration configuration, IHttpClientFactory clientFactory, IHttpContextAccessor httpContextAccessor)
            : base(nameof(ActivityRouterDialog))
        {
            AddDialog(new CardDialog(httpContextAccessor, clientFactory));
            AddDialog(new WaitForProactiveDialog(httpContextAccessor));
            AddDialog(new AttachmentDialog(new Uri($"{httpContextAccessor.HttpContext.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host.Value}")));
            AddDialog(new AuthDialog(configuration));
            AddDialog(new SsoSkillDialog(configuration));
            AddDialog(new EchoDialog());
            AddDialog(new FileUploadDialog());
            AddDialog(new DeleteDialog());
            AddDialog(new UpdateDialog());

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[] { ProcessActivityAsync }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ProcessActivityAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // A skill can send trace activities, if needed.
            await stepContext.Context.TraceActivityAsync($"{GetType().Name}.ProcessActivityAsync()", label: $"Got ActivityType: {stepContext.Context.Activity.Type}", cancellationToken: cancellationToken);

            switch (stepContext.Context.Activity.Type)
            {
                case ActivityTypes.Event:
                    return await OnEventActivityAsync(stepContext, cancellationToken);

                default:
                    // We didn't get an activity type we can handle.
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Unrecognized ActivityType: \"{stepContext.Context.Activity.Type}\".", inputHint: InputHints.IgnoringInput), cancellationToken);
                    return new DialogTurnResult(DialogTurnStatus.Complete);
            }
        }

        // This method performs different tasks based on the event name.
        private async Task<DialogTurnResult> OnEventActivityAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var activity = stepContext.Context.Activity;
            await stepContext.Context.TraceActivityAsync($"{GetType().Name}.OnEventActivityAsync()", label: $"Name: {activity.Name}. Value: {GetObjectAsJsonString(activity.Value)}", cancellationToken: cancellationToken);

            // Resolve what to execute based on the event name.
            switch (activity.Name)
            {
                case "Cards":
                    return await stepContext.BeginDialogAsync(FindDialog(nameof(CardDialog)).Id, cancellationToken: cancellationToken);

                case "Proactive":
                    return await stepContext.BeginDialogAsync(FindDialog(nameof(WaitForProactiveDialog)).Id, cancellationToken: cancellationToken);

                case "Attachment":
                    return await stepContext.BeginDialogAsync(FindDialog(nameof(AttachmentDialog)).Id, cancellationToken: cancellationToken);

                case "Auth":
                    return await stepContext.BeginDialogAsync(FindDialog(nameof(AuthDialog)).Id, cancellationToken: cancellationToken);

                case "Sso":
                    return await stepContext.BeginDialogAsync(FindDialog(nameof(SsoSkillDialog)).Id, cancellationToken: cancellationToken);

                case "Echo":
                    return await stepContext.BeginDialogAsync(FindDialog(nameof(EchoDialog)).Id, cancellationToken: cancellationToken);

                case "FileUpload":
                    return await stepContext.BeginDialogAsync(FindDialog(nameof(FileUploadDialog)).Id, cancellationToken: cancellationToken);

                case "Delete":
                    return await stepContext.BeginDialogAsync(FindDialog(nameof(DeleteDialog)).Id, cancellationToken: cancellationToken);

                case "Update":
                    return await stepContext.BeginDialogAsync(FindDialog(nameof(UpdateDialog)).Id, cancellationToken: cancellationToken);

                default:
                    // We didn't get an event name we can handle.
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Unrecognized EventName: \"{activity.Name}\".", inputHint: InputHints.IgnoringInput), cancellationToken);
                    return new DialogTurnResult(DialogTurnStatus.Complete);
            }
        }

        private string GetObjectAsJsonString(object value) => value == null ? string.Empty : JsonConvert.SerializeObject(value);
    }
}
