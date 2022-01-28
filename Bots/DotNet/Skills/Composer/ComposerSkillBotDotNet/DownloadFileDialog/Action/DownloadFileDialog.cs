// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace DownloadFileDialog.Action
{
    public class DownloadFileDialog : Dialog
    {
        [JsonProperty("$kind")]
        public const string Kind = "DownloadFileDialog";

        [JsonConstructor]
        public DownloadFileDialog([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            : base()
        {
            // enable instances of this command as debug break point
            RegisterSourceLocation(sourceFilePath, sourceLineNumber);
        }

        [JsonProperty("file")]
        public ObjectExpression<Attachment> File { get; set; }

        [JsonProperty("resultProperty")]
        public StringExpression ResultProperty { get; set; }

        public async override Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var file = File.GetValue(dc.State);

            var remoteFileUrl = file.ContentUrl;
            var localFileName = Path.Combine(Path.GetTempPath(), file.Name);
            string fileContent;

            using (var webClient = new WebClient())
            {
                webClient.DownloadFile(remoteFileUrl, localFileName);
                using (var reader = new StreamReader(localFileName))
                {
                    fileContent = await reader.ReadToEndAsync();
                }
            }

            if (ResultProperty != null)
            {
                dc.State.SetValue(ResultProperty.GetValue(dc.State), fileContent);
            }

            return await dc.EndDialogAsync(result: fileContent, cancellationToken: cancellationToken);
        }
    }
}
