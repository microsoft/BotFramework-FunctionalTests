// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TranscriptTestRunner;
using TranscriptTestRunner.XUnit;
using Xunit;
using Xunit.Abstractions;
using ActivityTypes = Microsoft.Bot.Connector.DirectLine.ActivityTypes;

namespace SkillFunctionalTests
{
    [Trait("TestCategory", "FunctionalTests")]
    [Trait("TestCategory", "OAuth")]
    [Trait("TestCategory", "SkipForV3Bots")]
    public class OAuthSkillTest
    {
        private readonly string _transcriptsFolder = Directory.GetCurrentDirectory() + @"/SourceTranscripts";
        private readonly ILogger<OAuthSkillTest> _logger;

        public OAuthSkillTest(ITestOutputHelper output)
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

            _logger = loggerFactory.CreateLogger<OAuthSkillTest>();
        }

        [Fact]
        public async Task ShouldSignIn()
        {
            var tries = 3;
            for (int index = 0; index < tries; index++)
            {
                try
                {
                    var runner = new XUnitTestRunner(new TestClientFactory(ClientType.DirectLine).GetTestClient(), _logger);
                    var signInUrl = string.Empty;

                    // Execute the first part of the conversation.
                    await runner.RunTestAsync(Path.Combine(_transcriptsFolder, "ShouldSignIn1.transcript"));

                    // Obtain the signIn url.
                    await runner.AssertReplyAsync(activity =>
                    {
                        Assert.Equal(ActivityTypes.Message, activity.Type);
                        Assert.True(activity.Attachments.Count > 0);

                        var card = JsonConvert.DeserializeObject<SigninCard>(JsonConvert.SerializeObject(activity.Attachments.FirstOrDefault().Content));
                        signInUrl = card.Buttons[0].Value?.ToString();

                        Assert.False(string.IsNullOrEmpty(signInUrl));
                    });

                    // Execute the SignIn.
                    await runner.ClientSignInAsync(signInUrl);

                    // Execute the rest of the conversation.
                    await runner.RunTestAsync(Path.Combine(_transcriptsFolder, "ShouldSignIn2.transcript"));
                }
                catch (TimeoutException timeoutException)
                {
                    if (index + 1 == tries)
                    {
                        throw timeoutException;
                    }
                    else
                    {
                        _logger.LogInformation($"======== Timeout exception on try number {index + 1}, starting retry... ========");
                    }
                }
            }
        }
    }
}
