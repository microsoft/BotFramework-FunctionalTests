using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkillFunctionalTests.Configuration
{
    public class EnvironmentBotTestConfiguration : IBotTestConfiguration
    {
        private const string DirectlineSecretKey = "DIRECTLINE";
        private const string BotIdKey = "BOTID";

        public EnvironmentBotTestConfiguration(string directLineSecretKey, string botIdKey)
        {
            // Load config from environment variables

            // DirectLine secret
            DirectLineSecret = Environment.GetEnvironmentVariable(directLineSecretKey);
            if (string.IsNullOrWhiteSpace(DirectLineSecret))
            {
                Assert.Inconclusive($"Environment variable '{directLineSecretKey}' not found.");
            }

            BotId = Environment.GetEnvironmentVariable(botIdKey);
            if (string.IsNullOrWhiteSpace(BotId))
            {
                Assert.Inconclusive($"Environment variable '{botIdKey}' not found.");
            }
        }

        public EnvironmentBotTestConfiguration() 
            : this(DirectlineSecretKey, BotIdKey)
        {
        }

        public string BotId { get; private set; }

        public string DirectLineSecret { get; private set; }
    }
}
