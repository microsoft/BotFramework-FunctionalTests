// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

            var section = configuration.GetSection("TestClientOptions");
            var testClientOptions = section.Get<TestClientOptions[]>();

            if (testClientOptions != null)
            {
                foreach (var option in testClientOptions)
                {
                    TestClientOptions.Add(option);
                }
            }
        }

        public static List<TestClientOptions> TestClientOptions { get; } = new List<TestClientOptions>();

        public ILogger Logger { get; }
    }
}
