// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;

namespace TranscriptTestRunner
{
    public abstract class TestClientBase
    {
        public abstract Task SendActivityAsync(Activity activity, CancellationToken cancellationToken);

        public abstract Task<Activity> GetNextReplyAsync(CancellationToken cancellationToken);

        public abstract Task SignInAsync(string signInUrl);
    }
}
