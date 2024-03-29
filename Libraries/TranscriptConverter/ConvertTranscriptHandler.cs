﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Testing.TranscriptConverter
{
    public class ConvertTranscriptHandler
    {
        /// <summary>
        /// Creates the convert command.
        /// </summary>
        /// <returns>The created command.</returns>
        public Command Create()
        {
            var cmd = new Command("convert", "Converts transcript files into test script to be executed by the TranscriptTestRunner");

            cmd.AddArgument(new Argument<string>("source"));
            cmd.AddOption(new Option<string>("--target", "Path to the target test script file."));

            cmd.Handler = CommandHandler.Create<string, string>((source, target) =>
            {
                try
                {
                    var rootStopwatch = new Stopwatch();
                    var stopwatch = new Stopwatch();
                    rootStopwatch.Start();

                    Console.WriteLine("Reading TranScript file\n  - Path: {0}", source);

                    var testScript = Converter.ConvertTranscript(source);

                    stopwatch.Start();
                    var targetPath = string.IsNullOrEmpty(target) ? source.Replace(".transcript", ".json", StringComparison.InvariantCulture) : target;

                    WriteTestScript(testScript, targetPath);

                    stopwatch.Stop();
                    Console.WriteLine("Saving TestScript file ({0}ms)\n  - Path: {1}", stopwatch.ElapsedMilliseconds, targetPath);

                    rootStopwatch.Stop();
                    Console.WriteLine("\nFinished processing ({0}ms)", rootStopwatch.ElapsedMilliseconds);
                }
                catch (FileNotFoundException e)
                {
                    Console.WriteLine("{0}: {1}", "Error", e.Message);
                }
                catch (DirectoryNotFoundException e)
                {
                    Console.WriteLine("{0}: {1}", "Error", e.Message);
                }
            });
            return cmd;
        }

        /// <summary>
        /// Writes the test script content to the path set in the target argument.
        /// </summary>
        private static void WriteTestScript(TestScript testScript, string targetScript)
        {
            var json = JsonConvert.SerializeObject(
                testScript,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

            using var streamWriter = new StreamWriter(Path.GetFullPath(targetScript));
            streamWriter.Write(json);
        }
    }
}
