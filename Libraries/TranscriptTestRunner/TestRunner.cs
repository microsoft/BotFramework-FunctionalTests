// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Activity = Microsoft.Bot.Schema.Activity;

namespace TranscriptTestRunner
{
    public class TestRunner
    {
        private readonly ILogger _logger;
        private readonly int _replyTimeout;
        private readonly TestClientBase _testClient;
        private Stopwatch _stopwatch;
        private TranscriptConverter _transcriptConverter;

        public TestRunner(TestClientBase client, ILogger logger = null)
        {
            _testClient = client;
            _replyTimeout = 45000;
            _logger = logger ?? NullLogger.Instance;
        }

        private Stopwatch Stopwatch
        {
            get
            {
                if (_stopwatch == null)
                {
                    _stopwatch = new Stopwatch();
                    _stopwatch.Start();
                }

                return _stopwatch;
            }
        }

        // TODO: Not sure if it is better to avoid this and have another constructor.
        public static async Task RunTestAsync(ClientType client, ILogger logger = null, params string[] transcriptPaths)
        {
            foreach (var transcriptPath in transcriptPaths)
            {
                // TODO: This should be outside of the loop
                var runner = new TestRunner(new TestClientFactory(client).GetTestClient(), logger);
                await runner.RunTestAsync(transcriptPath).ConfigureAwait(false);
            }
        }

        public async Task RunTestAsync(string transcriptPath, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"======== Running script: {transcriptPath} ========");
            ConvertTranscript(transcriptPath);
            await ExecuteTestScriptAsync(callerName, cancellationToken).ConfigureAwait(false);
        }

        public async Task SendActivityAsync(Activity sendActivity, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"[{Stopwatch.Elapsed}] User sends: {sendActivity.Text}.");
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
                        _logger.LogInformation($"[{Stopwatch.Elapsed}] Bot Responds: {activity.Text}.");
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

        protected virtual void AssertReply(TestScript expectedActivity, Activity actualActivity)
        {
            if (expectedActivity.Type != actualActivity.Type)
            {
                throw new Exception($"Invalid activity type. Expected: {expectedActivity.Type} Actual: {actualActivity.Type}");
            }

            if (expectedActivity.Text != actualActivity.Text)
            {
                throw new Exception($"Invalid activity text. Expected: {expectedActivity.Text} Actual: {actualActivity.Text}");
            }
        }

        private void ConvertTranscript(string transcriptPath)
        {
            _transcriptConverter = new TranscriptConverter
            {
                EmulatorTranscript = transcriptPath,
                TestScript = $"{Directory.GetCurrentDirectory()}/TestScripts/{Path.GetFileNameWithoutExtension(transcriptPath)}.json"
            };

            _transcriptConverter.Convert();
        }

        private async Task ExecuteTestScriptAsync(string callerName, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"\n------ Starting test {callerName} ----------");

            using var reader = new StreamReader(_transcriptConverter.TestScript);

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
                            var nextReply = await GetNextReplyAsync(cancellationToken).ConfigureAwait(false);
                            AssertReply(scriptActivity, nextReply);
                        }

                        break;

                    default:
                        throw new InvalidOperationException($"Invalid script activity type {scriptActivity.Role}.");
                }
            }

            _logger.LogInformation($"======== Finished running script: {Stopwatch.Elapsed} =============\n");
        }

        private bool IgnoreScriptActivity(TestScript activity)
        {
            return activity.Type == ActivityTypes.Trace || activity.Type == ActivityTypes.Typing;
        }
    }
}
