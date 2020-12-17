﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Sso
{
    public class SignOutDialog : ComponentDialog
    {
        private readonly string _connectionName;

        public SignOutDialog(IConfiguration configuration)
            : base(nameof(SignOutDialog))
        {
            _connectionName = configuration.GetSection("SsoConnectionName")?.Value;

            var steps = new WaterfallStep[] { SignOutAsync };

            AddDialog(new WaterfallDialog(nameof(SignInDialog), steps));
        }

        private async Task<DialogTurnResult> SignOutAsync(WaterfallStepContext context, CancellationToken cancellationToken)
        {
            var adapter = context.Context.Adapter as IUserTokenProvider;

            await adapter.SignOutUserAsync(context.Context, _connectionName);

            await context.Context.SendActivityAsync("You have been signed out of the skill app.");

            return await context.EndDialogAsync(null, cancellationToken);
        }
    }
}
