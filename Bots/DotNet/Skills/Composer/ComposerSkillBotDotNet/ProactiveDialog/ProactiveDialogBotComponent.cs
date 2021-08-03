using System.Collections.Concurrent;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ProactiveDialog
{
    public class ProactiveDialogBotComponent : BotComponent
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Anything that could be done in Startup.ConfigureServices can be done here.
            // In this case, the MultiplyDialog needs to be added as a new DeclarativeType.
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<ProactiveDialog>(ProactiveDialog.Kind));

            // Create the Conversation state. (Used by the Dialog system itself.)
            services.AddSingleton<ConversationState>();

            // Create a global dictionary for our ConversationReferences (used by proactive)
            services.AddSingleton<ConcurrentDictionary<string, ContinuationParameters>>();

            // Gives us access to HttpContext so we can create URLs with the host name.
            services.AddHttpContextAccessor();
        }
    }
}
