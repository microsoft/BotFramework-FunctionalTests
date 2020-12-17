﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Sso
{
    public class DisplayTokenDialog : ComponentDialog
    {
        private string _connectionName;

        public DisplayTokenDialog(IConfiguration configuration)
            : base(nameof(DisplayTokenDialog))
        {
            _connectionName = configuration.GetSection("SsoConnectionName")?.Value;

            var steps = new WaterfallStep[] { DisplayTokenAsync };

            AddDialog(new WaterfallDialog(nameof(SignInDialog), steps));
        }

        private async Task<DialogTurnResult> DisplayTokenAsync(WaterfallStepContext context, CancellationToken cancellationToken)
        {
            var adapter = context.Context.Adapter as IUserTokenProvider;

            var token = await adapter.GetUserTokenAsync(context.Context, _connectionName, null, cancellationToken);

            if (token == null)
            {
                await context.Context.SendActivityAsync("User has no cached token in the skill.");
            }
            else
            {
                await context.Context.SendActivityAsync($"Here is your token for the skill: {token.Token}");
            }

            return await context.EndDialogAsync(null, cancellationToken);
        }
    }
}
