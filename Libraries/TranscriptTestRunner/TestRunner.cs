// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Activity = Microsoft.Bot.Schema.Activity;

namespace TranscriptTestRunner
{
    /// <summary>
    /// Test runner implementation.
    /// </summary>
    public class TestRunner
    {
        private readonly ILogger _logger;
        private readonly int _replyTimeout;
        private readonly TestClientBase _testClient;
        private Stopwatch _stopwatch;
        private TranscriptConverter _transcriptConverter;
        private string _testScriptPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunner"/> class.
        /// </summary>
        /// <param name="client">Test client to use.</param>
        /// <param name="logger">Optional. Instance of <see cref="ILogger"/> to use.</param>
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

        /// <summary>
        /// Executes a test script with the test steps.
        /// </summary>
        /// <remarks>
        /// If the file is of type <i>.transcript</i> it will be converted to an intermediary <i>TestScript.json</i> file.
        /// </remarks>
        /// <param name="transcriptPath">Path to the file to use.</param>
        /// <param name="callerName">Optional. The name of the method caller.</param>
        /// <param name="cancellationToken">Optional. A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task RunTestAsync(string transcriptPath, [CallerMemberName] string callerName = "", CancellationToken cancellationToken = default)
        {
            var testFileName = $"{callerName} - {Path.GetFileNameWithoutExtension(transcriptPath)}";

            _logger.LogInformation($"======== Running script: {transcriptPath} ========");

            if (transcriptPath.EndsWith(".transcript", StringComparison.OrdinalIgnoreCase))
            {
                ConvertTranscript(transcriptPath);
            }
            else
            {
                _testScriptPath = transcriptPath;
            }

            await ExecuteTestScriptAsync(testFileName, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an <see cref="Activity"/> to the bot through the test client.
        /// </summary>
        /// <param name="sendActivity"><see cref="Activity"/> to send.</param>
        /// <param name="cancellationToken">Optional. A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task SendActivityAsync(Activity sendActivity, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Elapsed Time: {Elapsed}, User sends: {Text}", Stopwatch.Elapsed, sendActivity.Text);
            await _testClient.SendActivityAsync(sendActivity, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the next reply <see cref="Activity"/> from the bot through the test client.
        /// </summary>
        /// <param name="cancellationToken">Optional. A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>The reply Activity from the bot.</returns>
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
                        _logger.LogInformation("Elapsed Time: {Elapsed}, Bot Responds: {Text}", Stopwatch.Elapsed, activity.Text);
                        
                        if (activity.Attachments != null && activity.Attachments.Any())
                        {
                            foreach (var attachment in activity.Attachments)
                            {
                                _logger.LogInformation("Elapsed Time: {Elapsed}, Attachment included: {Type} - {Attachment}", Stopwatch.Elapsed, attachment.ContentType, attachment.Content);
                            }
                        }

                        return activity;
                    }

                    // Pop the next activity un the queue.
                    activity = await _testClient.GetNextReplyAsync(cancellationToken).ConfigureAwait(false);
                }
                while (activity != null);

                // Wait a bit for the bot
                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken).ConfigureAwait(false);

                if (timeoutCheck.ElapsedMilliseconds > _replyTimeout)
                {
                    throw new TimeoutException("operation timed out while waiting for a response from the bot");
                }
            }
        }

        /// <summary>
        /// Validates the reply <see cref="Activity"/> from the bot according to the validateAction parameter.
        /// </summary>
        /// <param name="validateAction">The <see cref="Action"/> to validate the reply <see cref="Activity"/> from the bot.</param>
        /// <param name="cancellationToken">Optional. A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task AssertReplyAsync(Action<Activity> validateAction, CancellationToken cancellationToken = default)
        {
            var nextReply = await GetNextReplyAsync(cancellationToken).ConfigureAwait(false);
            validateAction(nextReply);
        }

        /// <summary>
        /// Signs in to the bot through the test client.
        /// </summary>
        /// <param name="signInUrl">The sign in Url.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task ClientSignInAsync(string signInUrl)
        {
            if (string.IsNullOrEmpty(signInUrl))
            {
                throw new ArgumentNullException(signInUrl);
            }

            if (!signInUrl.StartsWith("https://", StringComparison.Ordinal))
            {
                throw new Exception($"Sign in url is badly formatted. Url received: {signInUrl}");
            }

            await _testClient.SignInAsync(signInUrl).ConfigureAwait(false);
        }

        /// <summary>
        /// Validates an <see cref="Activity"/> according to an expected activity <see cref="TestScriptItem"/>.
        /// </summary>
        /// <param name="expectedActivity">The expected activity of type <see cref="TestScriptItem"/>.</param>
        /// <param name="actualActivity">The actual response <see cref="Activity"/> received.</param>
        /// <param name="cancellationToken">Optional. A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        protected virtual Task AssertActivityAsync(TestScriptItem expectedActivity, Activity actualActivity, CancellationToken cancellationToken = default)
        {
            var templateRegex = new Regex(@"\{\{[\w\s]*\}\}");

            foreach (var assertion in expectedActivity.Assertions)
            {
                var template = templateRegex.Match(assertion);

                if (template.Success)
                {
                    ValidateVariable(template.Value, actualActivity);
                }

                var (result, error) = Expression.Parse(assertion).TryEvaluate<bool>(actualActivity);

                if (!result)
                {
                    throw new Exception($"Assertion failed: {assertion}.");
                }

                if (error != null)
                {
                    throw new Exception(error);
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Validates the variable date in the bots message with the value between double curly braces.
        /// </summary>
        /// <param name="value">The assertion containing the variable.</param>
        /// <param name="actualActivity">The activity with the message containing the date.</param>
        protected void ValidateVariable(string value, Activity actualActivity)
        {
            var dateRegex = new Regex(@"(\d{1,4}([.\-/])\d{1,2}([.\-/])\d{1,4})");
            var wordRegex = new Regex(@"[\w]+");

            var dateMatch = dateRegex.Match(actualActivity.Text);
            var resultExpression = string.Empty;
            var expectedExpression = wordRegex.Match(value).Value;
            var dateValue = string.Empty;
            
            if (dateMatch.Success)
            {
                dateValue = dateMatch.Value;
                var date = Convert.ToDateTime(dateMatch.Value, CultureInfo.InvariantCulture);
                resultExpression = EvaluateDate(date);
            }

            if (resultExpression != expectedExpression)
            {
                throw new Exception($"Assertion failed. The variable '{expectedExpression}' does not match with the value {dateValue}.");
            }

            actualActivity.Text = actualActivity.Text.Replace(dateMatch.Value, value);
        }

        private static string EvaluateDate(DateTime date)
        {
            var currentDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
            var inputDate = date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
            var expression = $"dateReadBack('{currentDate}', '{inputDate}')";
            var parsed = Expression.Parse(expression);

            if (parsed == null)
            {
                throw new Exception("Null parsed expression");
            }

            var (result, msg) = parsed.TryEvaluate(string.Empty);

            if (msg != null)
            {
                throw new Exception("An error has occurred while evaluating the date");
            }

            return result.ToString();
        }

        private void ConvertTranscript(string transcriptPath)
        {
            _transcriptConverter = new TranscriptConverter
            {
                EmulatorTranscript = transcriptPath,
                TestScript = $"{Directory.GetCurrentDirectory()}/TestScripts/{Path.GetFileNameWithoutExtension(transcriptPath)}.json"
            };

            _transcriptConverter.Convert();

            _testScriptPath = _transcriptConverter.TestScript;
        }

        private async Task ExecuteTestScriptAsync(string callerName, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"\n------ Starting test {callerName} ----------");

            using var reader = new StreamReader(_testScriptPath);

            var testScript = JsonConvert.DeserializeObject<TestScript>(await reader.ReadToEndAsync().ConfigureAwait(false));

            foreach (var scriptActivity in testScript.Items)
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
                        if (IgnoreScriptActivity(scriptActivity))
                        {
                            break;
                        }
                        
                        var nextReply = await GetNextReplyAsync(cancellationToken).ConfigureAwait(false);
                        await AssertActivityAsync(scriptActivity, nextReply, cancellationToken).ConfigureAwait(false);
                        break;

                    default:
                        throw new InvalidOperationException($"Invalid script activity type {scriptActivity.Role}.");
                }
            }

            _logger.LogInformation($"======== Finished running script: {Stopwatch.Elapsed} =============\n");
        }

        private bool IgnoreScriptActivity(TestScriptItem activity)
        {
            return activity.Type == ActivityTypes.Trace || activity.Type == ActivityTypes.Typing;
        }
    }
}
