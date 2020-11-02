// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

            var cleanedTranscript = RemoveUndesiredFields(transcript);

            var testScript = CreateTestScript(cleanedTranscript);

            WriteTestScript(testScript);
        }

        /// <summary>
        /// Recursively goes through the json content removing the undesired elements.
        /// </summary>
        /// <param name="token">The JToken element to process.</param>
        /// <param name="callback">The recursive function to iterate over the JToken.</param>
        private static void RemoveFields(JToken token, Func<JProperty, bool> callback)
        {
            if (!(token is JContainer container))
            {
                return;
            }

            var removeList = new List<JToken>();

            foreach (var element in container.Children())
            {
                if (element is JProperty prop && callback(prop))
                {
                    removeList.Add(element);
                }

                RemoveFields(element, callback);
            }

            foreach (var element in removeList)
            {
                element.Remove();
            }
        }

        /// <summary>
        /// Checks if a string is a GUID value.
        /// </summary>
        /// <param name="guid">The string to check.</param>
        /// <returns>True if the string is a GUID, otherwise, returns false.</returns>
        private static bool IsGuid(string guid)
        {
            var guidMatch = Regex.Match(
                guid,
                @"([a-z0-9]{8}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{12})",
                RegexOptions.IgnoreCase);
            return guidMatch.Success;
        }

        /// <summary>
        /// Checks if a string is an ID value.
        /// </summary>
        /// <param name="id">The string to check.</param>
        /// <returns>True if the string is an ID, otherwise, returns false.</returns>
        private static bool IsId(string id)
        {
            var idMatch = Regex.Match(
                id,
                @"([a-z0-9]{23})",
                RegexOptions.IgnoreCase);
            return idMatch.Success;
        }

        private static bool IsServiceUrl(string url)
        {
            var idMatch = Regex.Match(
                url,
                @"https://([a-z0-9]{12})",
                RegexOptions.IgnoreCase);
            return idMatch.Success;
        }

        private static bool IschannelId(string value)
        {
            return value.ToUpper(CultureInfo.InvariantCulture) == "EMULATOR";
        }

        /// <summary>
        /// Checks if a string is a JSON Object.
        /// </summary>
        /// <param name="value">The string to check.</param>
        /// <returns>True if the string is a JSON Object, otherwise, returns false.</returns>
        private static bool IsJsonObject(string value)
        {
            try
            {
                JsonConvert.DeserializeObject(value);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static List<TestScriptItem> CreateTestScript(string json)
        {
            var activities = JsonConvert.DeserializeObject<IEnumerable<Activity>>(json);
            var testScript = new List<TestScriptItem>();

            foreach (var activity in activities)
            {
                var scriptItem = new TestScriptItem
                {
                    Type = activity.Type,
                    Role = activity.From.Role,
                    Text = activity.Text
                };

                if (scriptItem.Role == "bot")
                {
                    var assertionsList = CreateAssertionsList(activity);

                    foreach (var assertion in assertionsList)
                    {
                        scriptItem.Assertions.Add(assertion);
                    }
                }

                testScript.Add(scriptItem);
            }

            return testScript;
        }

        private static IEnumerable<string> CreateAssertionsList(IActivity activity)
        {
            var json = JsonConvert.SerializeObject(
                activity,
                Formatting.None,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

            var token = JToken.Parse(json);
            var assertionsList = new List<string>();

            AddAssertions(token, assertionsList);

            return assertionsList;
        }

        private static void AddAssertions(JToken token, ICollection<string> assertionsList)
        {
            foreach (var property in token)
            {
                if (property is JProperty prop && !IsJsonObject(prop.Value.ToString()))
                {
                    if (prop.Path == "from.name")
                    {
                        continue;
                    }

                    var value = prop.Value.Type == JTokenType.String
                        ? $"'{prop.Value.ToString().Replace("'", "\\'")}'"
                        : prop.Value;

                    assertionsList.Add($"{prop.Path} == {value}");
                }
                else
                {
                    AddAssertions(property, assertionsList);
                }
            }
        }

        /// <summary>
        /// Removes the IDs and DateTime fields from the transcript file.
        /// </summary>
        /// <param name="transcript">The transcript file content.</param>
        /// <returns>The transcript content without the undesired fields.</returns>
        private string RemoveUndesiredFields(string transcript)
        {
            var token = JToken.Parse(transcript);

            RemoveFields(token, (attr) =>
            {
                var value = attr.Value.ToString();

                if (IsJsonObject(value))
                {
                    return false;
                }

                return IsGuid(value) || IsDateTime(value) || IsId(value) || IsServiceUrl(value) || IschannelId(value);
            });

            return token.ToString();
        }

        /// <summary>
        /// Checks if a string is a DateTime value.
        /// </summary>
        /// <param name="datetime">The string to check.</param>
        /// <returns>True if the string is a DateTime, otherwise, returns false.</returns>
        private bool IsDateTime(string datetime)
        {
            return DateTime.TryParse(datetime, out _);
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
        private void WriteTestScript(List<TestScriptItem> testScript)
        {
            var json = JsonConvert.SerializeObject(
                testScript,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

            using var streamWriter = new StreamWriter(TestScript);
            streamWriter.Write(json);
        }
    }
}
