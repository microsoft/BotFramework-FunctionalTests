using System.Security.Principal;
using Microsoft.Bot.Schema;

namespace ComposerSkillBotDotNet
{
    public class ContinuationParameters
    {
        public IIdentity ClaimsIdentity { get; set; }

        public string OAuthScope { get; set; }

        public ConversationReference ConversationReference { get; set; }
    }
}
