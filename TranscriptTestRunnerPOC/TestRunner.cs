// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace TranscriptTestRunnerPOC
{
    public class TestRunner
    {
        // public TestClientFactory TestClientFactory { get; set; }
        public TestClientBase TestClientBase { get; set; }

        public TranscriptConverter TranscriptConverter { get; set; }

        public TestRunner(TestClientBase client)
        {
            TestClientBase = client;
        }

        public async Task RunTestAsync(string transcriptPath)
        {
            ConvertTranscript(transcriptPath);

            await ExecuteTestScriptAsync();
        }

        private void ConvertTranscript(string transcriptPath)
        {
            TranscriptConverter = new TranscriptConverter
            {
                EmulatorTranscript = transcriptPath,
                TestScript = $"{Directory.GetCurrentDirectory()}/TestScripts/{Path.GetFileNameWithoutExtension(transcriptPath)}.json"
            };

            TranscriptConverter.Convert();
        }

        private async Task ExecuteTestScriptAsync()
        {
            using var reader = new StreamReader(TranscriptConverter.TestScript);

            //TODO: look how to deserialize this without an extra class.
            var testScript = JsonConvert.DeserializeObject<TestScript[]>(reader.ReadToEnd());

           foreach (var element in testScript)
           {
                if (element.Role == "user")
                {
                    var activity = new Activity
                    {
                        Type = ActivityTypes.Message,
                        Text = element.Text
                    };

                    await TestClientBase.SendActivityAsync(activity);
                }
                else
                {
                    var activity = new Activity
                    {
                        Type = ActivityTypes.Message,
                        Text = element.Text
                    };

                    if (!await TestClientBase.ValidateActivityAsync(activity))
                    {
                        throw new Exception($"The bot didn't reply as expected. It should have said: { activity.Text }");
                    }
                }
            }
        }
    }

    public class TestScript
    {
        public string Role;

        public string Text;
    }
}
