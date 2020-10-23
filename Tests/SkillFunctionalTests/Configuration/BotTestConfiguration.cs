// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Configuration;
using Xunit.Sdk;

namespace SkillFunctionalTests.Configuration
{
    /// <summary>
    /// Load test configuration from appsettings.json, appsettings.Development.json (if present) and environment variables.
    /// </summary>
    /// <remarks>
    /// This class will initialize the test configuration from config or environment variables.
    /// The values in appsettings will be overriden by the ones in appsettings.Development (if present) and those
    /// will be also overriden by the values in environment variables (if set).
    /// </remarks>
    public class BotTestConfiguration
    {
        private const string DirectLineSecretKey = "DIRECTLINE";
        private const string BotIdKey = "BOTID";
        private static readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes static members of the <see cref="BotTestConfiguration"/> class.
        /// Static constructor to initialize IConfiguration only once.
        /// </summary>
#pragma warning disable CA1810 // Initialize reference type static fields inline (To avoid breaking changes in current tests. This will be replaced later).
        static BotTestConfiguration()
#pragma warning restore CA1810 // Initialize reference type static fields inline
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", true, true)
                .AddEnvironmentVariables()
                .Build();
        }

        public BotTestConfiguration()
        {
            // DirectLine secret
            DirectLineSecret = _configuration[DirectLineSecretKey];
            if (string.IsNullOrWhiteSpace(DirectLineSecret))
            {
                throw new ArgumentException($"Configuration setting '{DirectLineSecret}' not found.");
            }

            BotId = _configuration[BotIdKey];
            if (string.IsNullOrWhiteSpace(BotId))
            {
                throw new ArgumentException($"Configuration setting '{BotIdKey}' not set.");
            }
        }

        public string BotId { get; }

        public string DirectLineSecret { get; }
    }
}
