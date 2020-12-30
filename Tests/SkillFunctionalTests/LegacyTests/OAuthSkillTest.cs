// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.DirectLine;
using Newtonsoft.Json;
using TranscriptTestRunner;
using TranscriptTestRunner.XUnit;
using Xunit;
using Xunit.Abstractions;
using ActivityTypes = Microsoft.Bot.Connector.DirectLine.ActivityTypes;

namespace SkillFunctionalTests.LegacyTests
{
    [Trait("TestCategory", "FunctionalTests")]
    [Trait("TestCategory", "OAuth")]
    [Trait("TestCategory", "SkipForV3Bots")]
    public class OAuthSkillTest : ScriptTestBase
    {
        private readonly string _transcriptsFolder = Directory.GetCurrentDirectory() + @"/LegacyTests/SourceTranscripts";

        public OAuthSkillTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact]
        public async Task ShouldSignIn()
        {
            var runner = new XUnitTestRunner(new TestClientFactory(ClientType.DirectLine, TestClientOptions[0]).GetTestClient(), Logger);
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
    }
}
