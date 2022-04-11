// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace TranscriptConverter
{
    public static class Converter
    {
        private static readonly HashSet<JTokenType> IgnoreTypes = new HashSet<JTokenType>
        {
            JTokenType.Array,
            JTokenType.Object
        };

        /// <summary>
        /// Converts the transcript into a test script.
        /// </summary>
        /// <param name="transcriptPath">The path to the transcript file.</param>
        /// <returns>The test script created.</returns>
        public static TestScript ConvertTranscript(string transcriptPath)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            using var reader = new StreamReader(Path.GetFullPath(transcriptPath));

            var transcript = reader.ReadToEnd();
            var json = JToken.Parse(transcript);
            var cleanedTranscript = RemoveUndesiredFields(json);

            stopwatch.Stop();
            Console.WriteLine("Removing undesired fields ({0}ms)", stopwatch.ElapsedMilliseconds);
            stopwatch.Reset();
            stopwatch.Start();

            var result = CreateTestScript(cleanedTranscript);

            stopwatch.Stop();
            Console.WriteLine("Creating TestScript ({0}ms)", stopwatch.ElapsedMilliseconds);

            return result;
        }

        /// <summary>
        /// Removes variable fields like IDs, Dates and urls from the transcript file.
        /// </summary>
        /// <param name="json">The transcript file content.</param>
        /// <returns>The transcript content without the undesired fields.</returns>
        private static JToken RemoveUndesiredFields(JToken json)
        {
            if (!(json is JContainer container))
            {
                return null;
            }

            static bool ShouldRemove(HashSet<string> processedProps, JProperty prop)
            {
                var key = prop.Name;
                var value = prop.Value.ToString();

                if (processedProps.Contains(key))
                {
                    return true;
                }

                var result = string.IsNullOrEmpty(value)
                    || IsDateTime(value)
                    || IsUrl(value)
                    || IsBase64Image(value)
                    || IsId(key, value);

                if (result)
                {
                    processedProps.Add(key);
                }

                return result;
            }

            var processedProps = new HashSet<string>();

            container.DescendantsAndSelf()
                .OfType<JProperty>()
                .Where(prop => !IgnoreTypes.Contains(prop.Value.Type))
                .Where(prop => ShouldRemove(processedProps, prop))
                .ToList()
                .ForEach(prop => prop.Remove());

            return container;
        }

        /// <summary>
        /// Creates the test script based on the transcript's activities.
        /// </summary>
        /// <param name="json">Json containing the transcript's activities.</param>
        /// <returns>The test script created.</returns>
        private static TestScript CreateTestScript(JToken json)
        {
            if (!(json is JContainer container))
            {
                return null;
            }

            var ignoreFields = new HashSet<string>
            {
                "from.name"
            };

            static string MapAssert(JProperty prop)
            {
                var value = prop.Value.Type == JTokenType.String
                    ? $"'{prop.Value.ToString().Replace("'", "\\'", StringComparison.InvariantCulture)}'"
                    : prop.Value;

                return $"{prop.Path} == {value}";
            }

            static TestScriptItem MapItem(JToken obj, HashSet<JTokenType> ignoreTypes, HashSet<string> ignoreFields)
            {
                // Detach from the container.
                var activityJson = JToken.Parse(obj.ToString());
                var assertions = new List<string>();

                var role = activityJson["from"].Value<string>("role");
                if (role == "bot")
                {
                    assertions = (activityJson as JContainer)
                        .DescendantsAndSelf()
                        .OfType<JProperty>()
                        .Where(prop => !ignoreTypes.Contains(prop.Value.Type))
                        .Where(prop => !ignoreFields.Contains(prop.Path))
                        .Select(MapAssert)
                        .ToList();
                }

                return new TestScriptItem(assertions)
                {
                    Type = activityJson.Value<string>("type"),
                    Role = role,
                    Text = activityJson.Value<string>("text"),
                };
            }

            var items = container.Select(obj => MapItem(obj, IgnoreTypes, ignoreFields)).ToList();

            return new TestScript(items);
        }

        /// <summary>
        /// Checks if a string is a Base64 image.
        /// </summary>
        /// <param name="base64">The string to check.</param>
        /// <returns>True if the string is a Base64 image, otherwise, returns false.</returns>
        private static bool IsBase64Image(string base64)
        {
            var base64Match = Regex.Match(
                base64,
                @"^data:image\/png;base64,",
                RegexOptions.IgnoreCase);
            return base64Match.Success;
        }

        /// <summary>
        /// Checks if a property is an ID value.
        /// </summary>
        /// <param name="property">The ID property name to check.</param>
        /// <param name="value">The ID property value to check.</param>
        /// <returns>True if the string is an ID, otherwise, returns false.</returns>
        private static bool IsId(string property, string value)
        {
            var specialCharactersMatch = Regex.Match(value, @"\r|\n|\t");

            var guidMatch = Regex.Match(
                value,
                @"^([a-z0-9]{8}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{12})$",
                RegexOptions.IgnoreCase);

            var idPropertyMatch = Regex.Match(
                property,
                @"^id|id$",
                RegexOptions.IgnoreCase);

            return !specialCharactersMatch.Success && (guidMatch.Success || idPropertyMatch.Success);
        }

        /// <summary>
        /// Checks if a string is an url value.
        /// </summary>
        /// <remarks>
        /// Evaluates if the value starts with udp://, ftp://, http://, https://, etc.
        /// </remarks>
        /// <param name="url">The string to check.</param>
        /// <returns>True if the string is an url, otherwise, returns false.</returns>
        private static bool IsUrl(string url)
        {
            var idMatch = Regex.Match(
                url,
                @"^[a-z]*:\/\/",
                RegexOptions.IgnoreCase);
            return idMatch.Success;
        }

        /// <summary>
        /// Checks if a string is a DateTime value.
        /// </summary>
        /// <param name="datetime">The string to check.</param>
        /// <returns>True if the string is a DateTime, otherwise, returns false.</returns>
        private static bool IsDateTime(string datetime)
        {
            var dateMatch = DateTime.TryParse(datetime, out _);
            return dateMatch;
        }
    }
}
