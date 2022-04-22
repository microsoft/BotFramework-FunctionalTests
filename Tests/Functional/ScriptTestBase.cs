// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using Microsoft.Bot.Builder.Testing.TestRunner.TestClients;
using Microsoft.Bot.Builder.Tests.Functional.Skills.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.Bot.Builder.Tests.Functional
{
    public class ScriptTestBase
    {
        public ScriptTestBase(ITestOutputHelper output)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConfiguration(configuration.GetSection("Logging"))
                    .AddConsole()
                    .AddDebug()
                    .AddFile(Directory.GetCurrentDirectory() + @"/Logs/Log.json", isJson: true)
                    .AddXunit(output);
            });

            Logger = loggerFactory.CreateLogger<ScriptTestBase>();

            TestRequestTimeout = int.Parse(configuration["TestRequestTimeout"]);
            TestClientOptions = configuration.GetSection("HostBotClientOptions").Get<Dictionary<HostBot, DirectLineTestClientOptions>>();
            ThinkTime = int.Parse(configuration["ThinkTime"]);
        }

        public static List<string> Channels { get; } = new List<string>
        {
            Connector.Channels.Directline
        };

        public Dictionary<HostBot, DirectLineTestClientOptions> TestClientOptions { get; }

        public ILogger Logger { get; }

        public int TestRequestTimeout { get; }

        public int ThinkTime { get; }
    }
}
