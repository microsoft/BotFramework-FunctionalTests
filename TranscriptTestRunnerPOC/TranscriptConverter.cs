// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Bot.Schema;

namespace TranscriptTestRunnerPOC
{
    /// <summary>
    /// Class for converting a transcript into a Test Script file.
    /// </summary>
    public class TranscriptConverter
    {
        /// <summary>
        /// Gets or sets the path to the emulator transcript file.
        /// </summary>
        /// <value>The path to the emulator transcript file.</value>
        public string EmulatorTranscript { get; set; }

        /// <summary>
        /// Gets or sets the resulting test script.
        /// </summary>
        /// <value>The resulting test script.</value>
        public string TestScript { get; set; }

        /// <summary>
        /// Converts the .transcript file in a test script.
        /// </summary>
        public void Convert()
        {
            if (string.IsNullOrWhiteSpace(EmulatorTranscript))
            {
                throw new Exception($"{nameof(EmulatorTranscript)} property not set");
            }

            if (!File.Exists(EmulatorTranscript))
            {
                throw new Exception($"{nameof(EmulatorTranscript)}: {EmulatorTranscript} path does not exist");
            }

            var transcript = ReadEmulatorTranscript();
            var activities = JsonConvert.DeserializeObject<Activity[]>(transcript);

            TestScript = CreateTestScript(activities);
        }

        private static string CreateTestScript(IEnumerable<Activity> transcript)
        {
            var scriptArray = new JArray();


            foreach (var activity in transcript)
            {
                if (activity.Type == ActivityTypes.Message)
                {
                    var script = new JObject
                    {
                        { "role", activity.From.Role },
                        { "text", activity.Text }
                    };
                
                    scriptArray.Add(script);
                }
            }
            
            return JsonConvert.SerializeObject(scriptArray);
        }

        /// <summary>
        /// Reads the emulator transcript file.
        /// </summary>
        /// <returns>A string with the transcript content.</returns>
        private string ReadEmulatorTranscript()
        {
            using var reader = new StreamReader(EmulatorTranscript);
            return reader.ReadToEnd();
        }
    }
}
