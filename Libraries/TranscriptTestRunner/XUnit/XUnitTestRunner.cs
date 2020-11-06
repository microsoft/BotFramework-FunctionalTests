// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;

namespace TranscriptTestRunner.XUnit
{
    public class XUnitTestRunner : TestRunner
    {
        public XUnitTestRunner(TestClientBase client, ILogger logger = null)
            : base(client, logger)
        {
        }

        protected override Task AssertActivityAsync(TestScriptItem expectedActivity, Activity actualActivity, CancellationToken cancellationToken = default)
        {
            foreach (var assertion in expectedActivity.Assertions)
            {
                var (result, error) = Expression.Parse(assertion).TryEvaluate<bool>(actualActivity);

                Assert.True(result, $"The bot's response was different than expected. The assertion: \"{assertion}\" was evaluated as false.\nActual Activity:\n{JsonConvert.SerializeObject(actualActivity, Formatting.Indented)}");
            }

            return Task.CompletedTask;
        }
    }
}
