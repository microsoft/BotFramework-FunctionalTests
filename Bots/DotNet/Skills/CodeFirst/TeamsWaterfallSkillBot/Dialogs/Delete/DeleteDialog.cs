// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace Microsoft.BotFrameworkFunctionalTests.TeamsWaterfallSkillBot.Dialogs.Delete
{
    public class DeleteDialog : ComponentDialog
    {
        public DeleteDialog()
             : base(nameof(DeleteDialog))
        {
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[] { HandleDeleteDialog }));
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> HandleDeleteDialog(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var channel = stepContext.Context.Activity.ChannelId;
            if (DeleteSupportedInChannel.IsDeleteSupported(channel))
            {
                var id = await stepContext.Context.SendActivityAsync(MessageFactory.Text("I will delete this message in 5 seconds"), cancellationToken);
                Thread.Sleep(5000);
                await stepContext.Context.DeleteActivityAsync(id.Id, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Delete is not supported in the {channel} channel."), cancellationToken);
            }

            return new DialogTurnResult(DialogTurnStatus.Complete);
        }
    }
}
