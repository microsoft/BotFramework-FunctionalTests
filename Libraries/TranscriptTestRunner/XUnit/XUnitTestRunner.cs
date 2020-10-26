// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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

        protected override void AssertReply(TestScript expectedActivity, Activity actualActivity)
        {
            Assert.Equal(expectedActivity.Type, actualActivity.Type);
            Assert.Equal(expectedActivity.Text, actualActivity.Text);
        }
    }
}
