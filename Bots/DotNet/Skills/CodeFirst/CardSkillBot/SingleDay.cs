using Newtonsoft.Json;

namespace Microsoft.BotFrameworkFunctionalTests.CardSkillBot
{
    public class SingleDay
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("temperature")]
        public string Temperature { get; set; }

        [JsonProperty("shortForecast")]
        public string ShortForecast { get; set; }
    }
}
