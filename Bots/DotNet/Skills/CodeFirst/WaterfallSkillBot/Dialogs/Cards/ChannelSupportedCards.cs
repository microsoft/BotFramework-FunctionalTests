using System.Collections.Generic;
using Microsoft.Bot.Connector;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Cards
{
    public static class ChannelSupportedCards
    {
        /// <summary>
        /// This tracks what cards are not supported in a given channel.
        /// </summary>
        private static readonly Dictionary<string, List<CardOptions>> UnsupportedChannelCards = new Dictionary<string, List<CardOptions>> 
        {
            {
                Channels.Emulator, new List<CardOptions>
                {
                    CardOptions.TeamsFileConsent,
                    CardOptions.O365
                }
            }
        };

        /// <summary>
        /// This let's you know if a card is supported in a given channel.
        /// </summary>
        /// <param name="channel">Bot Connector Channel.</param>
        /// <param name="type">Card Option to be checked.</param>
        /// <returns>A bool if the card is supported in the channel.</returns>
        public static bool IsCardSupported(string channel, CardOptions type)
        {
            if (UnsupportedChannelCards.ContainsKey(channel.ToString()))
            {
                if (UnsupportedChannelCards[channel.ToString()].Contains(type))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
