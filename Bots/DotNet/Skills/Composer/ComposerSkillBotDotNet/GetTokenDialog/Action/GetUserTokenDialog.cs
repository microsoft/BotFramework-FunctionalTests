// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector.Authentication;
using Newtonsoft.Json;

namespace GetUserTokenDialog.Action
{
    public class GetUserTokenDialog : Dialog
    {
        [JsonProperty("$kind")]
        public const string Kind = "GetUserTokenDialog";

        [JsonConstructor]
        public GetUserTokenDialog([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            // enable instances of this command as debug break point
            RegisterSourceLocation(sourceFilePath, sourceLineNumber);
        }

        [JsonProperty("resultProperty")]
        public StringExpression ResultProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var connectionName = dc.Parent.State.GetValue<string>("settings.SsoConnectionName");

            var userTokenClient = dc.Context.TurnState.Get<UserTokenClient>();

            var token = await userTokenClient.GetUserTokenAsync(dc.Context.Activity.From.Id, connectionName, dc.Context.Activity.ChannelId, null, cancellationToken).ConfigureAwait(false);

            if (token != null)
            {
                dc.State.SetValue(ResultProperty.GetValue(dc.State), token.Token);
            }

            return await dc.EndDialogAsync(result: token, cancellationToken: cancellationToken);
        }
    }
}
