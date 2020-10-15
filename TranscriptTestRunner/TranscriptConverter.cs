// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Bot.Schema;

namespace TranscriptTestRunner
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
        /// Gets or sets the path to the resulting test script file.
        /// </summary>
        /// <value>The path to the resulting test script file.</value>
        public string TestScript { get; set; }

        /// <summary>
        /// Converts the .transcript file in a test script.
        /// </summary>
        public void Convert()
        {
            ValidatePaths();

            var transcript = ReadEmulatorTranscript();
            var activities = JsonConvert.DeserializeObject<Activity[]>(transcript);
            var testScript = CreateTestScript(activities);

            WriteTestScript(testScript);
        }

        /// <summary>
        /// Creates the test script based on the transcript content.
        /// </summary>
        /// <param name="transcript">The .transcript content parsed as Activities.</param>
        /// <returns>A string representing the test script json content.</returns>
        private static string CreateTestScript(IEnumerable<Activity> transcript)
        {
            var scriptArray = new JArray();

            foreach (var activity in transcript)
            {
                var script = new JObject
                {
                    { "type", activity.Type },
                    { "role", activity.From.Role },
                    { "text", activity.Text }
                };

                scriptArray.Add(script);
            }

            return JsonConvert.SerializeObject(scriptArray);
        }

        /// <summary>
        /// Validates the paths for the transcript and the test script files.
        /// </summary>
        private void ValidatePaths()
        {
            if (string.IsNullOrWhiteSpace(EmulatorTranscript))
            {
                throw new Exception($"{nameof(EmulatorTranscript)} property not set");
            }

            if (!File.Exists(EmulatorTranscript))
            {
                throw new Exception($"{nameof(EmulatorTranscript)}: {EmulatorTranscript} path does not exist");
            }

            if (string.IsNullOrWhiteSpace(TestScript))
            {
                throw new Exception($"{nameof(TestScript)} property not set");
            }

            var testScriptFolder = Path.GetDirectoryName(TestScript);

            if (!Directory.Exists(testScriptFolder))
            {
                Directory.CreateDirectory(testScriptFolder);
            }

            if (!File.Exists(TestScript))
            {
                using var fs = File.Create(TestScript);
            }
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

        /// <summary>
        /// Writes the test script content to the path set in the TestScript property.
        /// </summary>
        /// <param name="json">The test script content to be written.</param>
        private void WriteTestScript(string json)
        {
            using var streamWriter = new StreamWriter(TestScript);
            streamWriter.Write(json);
        }
    }
}
