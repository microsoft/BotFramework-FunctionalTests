// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Activity = Microsoft.Bot.Schema.Activity;

namespace TranscriptTestRunner
{
    public class TestRunner
    {
        private readonly int _replyTimeout;
        private readonly TestClientBase _testClient;
        private Stopwatch _stopwatch;

        public TestRunner(TestClientBase client)
        {
            _testClient = client;
            _replyTimeout = 45000;
        }

        private TranscriptConverter TranscriptConverter { get; set; }

        // TODO: Not sure if it is better to avoid this and have another constructor.
        public static async Task RunTestAsync(ClientType client, params string[] transcriptPaths)
        {
            foreach (var transcriptPath in transcriptPaths)
            {
                // TODO: This should be outside of the loop
                var runner = new TestRunner(new TestClientFactory(client).GetTestClient());
                await runner.RunTestAsync(transcriptPath).ConfigureAwait(false);
            }
        }

        public async Task RunTestAsync(string transcriptPath, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default)
        {
            ConvertTranscript(transcriptPath);
            await ExecuteTestScriptAsync(callerName, cancellationToken).ConfigureAwait(false);
        }

        public async Task SendActivityAsync(Activity sendActivity, CancellationToken cancellationToken = default)
        {
            Debug.WriteLine($"[{_stopwatch.Elapsed}] User sends: {sendActivity.Text}");
            await _testClient.SendActivityAsync(sendActivity, cancellationToken).ConfigureAwait(false);
        }

        public async Task<Activity> GetNextReplyAsync(CancellationToken cancellationToken = default)
        {
            var timeoutCheck = new Stopwatch();
            timeoutCheck.Start();
            while (true)
            {
                var activity = await _testClient.GetNextReplyAsync(cancellationToken).ConfigureAwait(false);
                do
                {
                    if (activity != null && activity.Type != ActivityTypes.Trace && activity.Type != ActivityTypes.Typing)
                    {
                        Debug.WriteLine($"[{_stopwatch.Elapsed}] Bot Responds: {activity.Text}");
                        return activity;
                    }

                    // Pop the next activity un the queue.
                    activity = await _testClient.GetNextReplyAsync(cancellationToken).ConfigureAwait(false);
                } 
                while (activity != null);

                // Wait a bit for the bot
                await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken).ConfigureAwait(false);

                if (timeoutCheck.ElapsedMilliseconds > _replyTimeout)
                {
                    throw new TimeoutException("operation timed out while waiting for a response from the bot");
                }
            }
        }

        public async Task AssertReplyAsync(Action<Activity> validateAction, CancellationToken cancellationToken = default)
        {
            var nextReply = await GetNextReplyAsync(cancellationToken).ConfigureAwait(false);
            validateAction(nextReply);
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

        private async Task ExecuteTestScriptAsync(string callerName, CancellationToken cancellationToken)
        {
            Debug.WriteLine($"\n------ Starting test {callerName} ----------");
            _stopwatch = new Stopwatch();
            _stopwatch.Start();

            using var reader = new StreamReader(TranscriptConverter.TestScript);

            var testScript = JsonConvert.DeserializeObject<TestScript[]>(await reader.ReadToEndAsync().ConfigureAwait(false));

            foreach (var scriptActivity in testScript)
            {
                switch (scriptActivity.Role)
                {
                    case RoleTypes.User:
                        // Send the activity.
                        var sendActivity = new Activity
                        {
                            Type = scriptActivity.Type,
                            Text = scriptActivity.Text
                        };

                        await SendActivityAsync(sendActivity, cancellationToken).ConfigureAwait(false);
                        break;

                    case RoleTypes.Bot:
                        // Assert the activity returned
                        if (!IgnoreScriptActivity(scriptActivity))
                        {
                            await AssertReplyAsync(
                                nextReply =>
                                {
                                    if (scriptActivity.Type != nextReply.Type)
                                    {
                                        throw new Exception($"Invalid activity type. Expected: {scriptActivity.Type} Actual: {nextReply.Type}");
                                    }

                                    if (scriptActivity.Text != nextReply.Text)
                                    {
                                        throw new Exception($"Invalid activity text. Expected: {scriptActivity.Text} Actual: {nextReply.Text}");
                                    }
                                },
                                cancellationToken).ConfigureAwait(false);
                        }

                        break;

                    default:
                        throw new InvalidOperationException($"Invalid script activity type {scriptActivity.Role}.");
                }
            }

            Debug.WriteLine($"======== Test run finished in: {_stopwatch.Elapsed} =============\n");
            _stopwatch.Stop();
        }

        private bool IgnoreScriptActivity(TestScript activity)
        {
            return activity.Type == ActivityTypes.Trace || activity.Type == ActivityTypes.Typing;
        }
    }
}
