// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Bot.Schema;

namespace TranscriptTestRunnerPOC
{
    public abstract class TestClientBase
    {
        public abstract Task SendActivityAsync(Activity activity);

        public abstract Task<bool> ValidateActivityAsync(Activity expected);
    }
}
