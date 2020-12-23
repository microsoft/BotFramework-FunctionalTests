using System.Collections.Generic;
using Microsoft.Bot.Connector;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Cards
{
    public static class ChannelSupportedCards
    {
        private static readonly Dictionary<string, List<CardTypes>> Dict = new Dictionary<string, List<CardTypes>> 
        {
            { 
                Channels.Directline, new List<CardTypes> 
                { 
                    CardTypes.Hero,
                    CardTypes.Animation,
                    CardTypes.Audio,
                    CardTypes.BotAction
                } 
            },
            {
                Channels.Emulator, new List<CardTypes>
                {
                    CardTypes.Hero
                }
            }
        };

        public static bool CardSupported(string channel, CardTypes type)
        {
            if (Dict.ContainsKey(channel.ToString()))
            {
                if (Dict[channel.ToString()].Contains(type))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
