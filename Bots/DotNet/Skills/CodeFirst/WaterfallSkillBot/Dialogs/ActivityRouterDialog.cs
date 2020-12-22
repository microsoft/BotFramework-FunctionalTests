// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Schema;
using Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Attachments;
using Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Auth;
using Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Cards;
using Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Proactive;
using Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Sso;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs
{
    /// <summary>
    /// A root dialog that can route activities sent to the skill to different sub-dialogs.
    /// </summary>
    public class ActivityRouterDialog : ComponentDialog
    {
        public ActivityRouterDialog(IConfiguration configuration, IHttpClientFactory clientFactory, IHttpContextAccessor httpContextAccessor)
            : base(nameof(ActivityRouterDialog))
        {
            AddDialog(new CardDialog(clientFactory));
            AddDialog(new WaitForProactiveDialog(httpContextAccessor));
            AddDialog(new AttachmentDialog());
            AddDialog(new AuthDialog(configuration));
            AddDialog(new SsoSkillDialog(configuration));

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

                default:
                    // We didn't get an event name we can handle.
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Unrecognized EventName: \"{activity.Name}\".", inputHint: InputHints.IgnoringInput), cancellationToken);
                    return new DialogTurnResult(DialogTurnStatus.Complete);
            }
        }

        private string GetObjectAsJsonString(object value) => value == null ? string.Empty : JsonConvert.SerializeObject(value);
    }
}
