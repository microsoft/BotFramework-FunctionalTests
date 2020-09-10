// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Xunit.Sdk;

namespace SkillFunctionalTests.Configuration
{
    public class EnvironmentBotTestConfiguration : IBotTestConfiguration
    {
        private const string DirectLineSecretKey = "DIRECTLINE";
        private const string BotIdKey = "BOTID";

        public EnvironmentBotTestConfiguration(string directLineSecretKey, string botIdKey)
        {
            // Load config from environment variables

            // DirectLine secret
            DirectLineSecret = Environment.GetEnvironmentVariable(directLineSecretKey);
            if (string.IsNullOrWhiteSpace(DirectLineSecret))
            {
                throw new XunitException($"Environment variable '{directLineSecretKey}' not found.");
            }

            BotId = Environment.GetEnvironmentVariable(botIdKey);
            if (string.IsNullOrWhiteSpace(BotId))
            {
                throw new XunitException($"Environment variable '{botIdKey}' not found.");
            }
        }

        public EnvironmentBotTestConfiguration() 
            : this(DirectLineSecretKey, BotIdKey)
        {
        }

        public string BotId { get; }

        public string DirectLineSecret { get; }
    }
}
