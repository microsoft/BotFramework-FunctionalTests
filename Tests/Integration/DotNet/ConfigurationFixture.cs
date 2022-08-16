// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Bot.Builder.Tests.Integration
{
    public class ConfigurationFixture
    {
        public ConfigurationFixture()
        {
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            Timeout = TimeSpan.FromSeconds(3);
        }

        public IConfigurationRoot Configuration { get; private set; }

        public TimeSpan Timeout { get; private set; }
    }
}
