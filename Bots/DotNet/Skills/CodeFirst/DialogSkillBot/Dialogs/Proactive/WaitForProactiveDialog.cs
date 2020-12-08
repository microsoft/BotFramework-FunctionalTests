// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Proactive
{
    public class WaitForProactiveDialog : Dialog
    {
        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = new CancellationToken())
        {
            await dc.Context.SendActivityAsync(MessageFactory.Text("Visit localhost:[PORT]/api/notify to receive a proactive message."), cancellationToken);
            return EndOfTurn;
        }

        public override Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = new CancellationToken())
        {
            return base.ContinueDialogAsync(dc, cancellationToken);
        }
    }
}
