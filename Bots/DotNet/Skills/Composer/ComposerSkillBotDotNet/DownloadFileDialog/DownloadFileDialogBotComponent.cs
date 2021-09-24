// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DownloadFileDialog
{
    public class DownloadFileDialogBotComponent : BotComponent
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Anything that could be done in Startup.ConfigureServices can be done here.
            // In this case, the DownloadFileDialog needs to be added as a new DeclarativeType.
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<Action.DownloadFileDialog>(Action.DownloadFileDialog.Kind));
        }
    }
}
