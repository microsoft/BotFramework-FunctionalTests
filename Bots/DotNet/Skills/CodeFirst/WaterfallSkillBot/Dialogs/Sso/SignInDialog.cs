// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Sso
{
    public class SignInDialog : ComponentDialog
    {
        public SignInDialog(IConfiguration configuration)
            : base(nameof(SignInDialog))
        {
            var connectionName = configuration.GetSection("SsoConnectionName")?.Value;

            AddDialog(new OAuthPrompt(nameof(OAuthPrompt), new OAuthPromptSettings
            {
                ConnectionName = connectionName,
                Text = "Sign In to AAD for the Skill",
                Title = "Sign In"
            }));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[] { SignInStepAsync, DisplayTokenAsync }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> SignInStepAsync(WaterfallStepContext context, CancellationToken cancellationToken)
        {
            var userid = context.Context.Activity.From.Id;
            return await context.BeginDialogAsync(nameof(OAuthPrompt), null, cancellationToken);
        }

        private async Task<DialogTurnResult> DisplayTokenAsync(WaterfallStepContext context, CancellationToken cancellationToken)
        {
            if (!(context.Result is TokenResponse result))
            {
                await context.Context.SendActivityAsync("No token was provided for the skill.", cancellationToken: cancellationToken);
            }
            else
            {
                await context.Context.SendActivityAsync($"Here is your token for the skill: {result.Token}", cancellationToken: cancellationToken);
            }

            return await context.EndDialogAsync(null, cancellationToken);
        }
    }
}
