// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkillFunctionalTests.Common;
using TranscriptTestRunner.TestClients;
using Xunit.Abstractions;

namespace SkillFunctionalTests
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
                    .AddConfiguration(configuration)
                    .AddConsole()
                    .AddDebug()
                    .AddFile(Directory.GetCurrentDirectory() + @"/Logs/Log.json", isJson: true)
                    .AddXunit(output);
            });

            Logger = loggerFactory.CreateLogger<ScriptTestBase>();

            TestClientOptions = configuration.GetSection("HostBotClientOptions").Get<Dictionary<HostBot, DirectLinetTestClientOptions>>();
        }

        public static Dictionary<HostBot, DirectLinetTestClientOptions> TestClientOptions { get; private set; }

        public ILogger Logger { get; }
    }
}
