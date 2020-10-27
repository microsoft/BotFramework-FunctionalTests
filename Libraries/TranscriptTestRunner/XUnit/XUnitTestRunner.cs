// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
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
            Assert.Equal(expectedActivity.Type, actualActivity.Type);
            Assert.Equal(expectedActivity.Text, actualActivity.Text);

            return Task.CompletedTask;
        }
    }
}
