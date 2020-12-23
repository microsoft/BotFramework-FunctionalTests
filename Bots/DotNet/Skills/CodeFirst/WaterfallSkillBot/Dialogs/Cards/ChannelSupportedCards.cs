using System.Collections.Generic;
using Microsoft.Bot.Connector;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Cards
{
    public static class ChannelSupportedCards
    {
        private static readonly Dictionary<string, List<CardOptions>> UnsupportedCards = new Dictionary<string, List<CardOptions>> 
        {
            { 
                Channels.Directline, new List<CardOptions> 
                { 
                    CardOptions.Hero,
                    CardOptions.Animation,
                    CardOptions.Audio,
                    CardOptions.AdaptiveCardBotAction
                } 
            },
            {
                Channels.Emulator, new List<CardOptions>
                {
                    CardOptions.TeamsFileConsent,
                    CardOptions.O365
                }
            }
        };

        public static bool CardSupported(string channel, CardOptions type)
        {
            if (UnsupportedCards.ContainsKey(channel.ToString()))
            {
                if (UnsupportedCards[channel.ToString()].Contains(type))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
