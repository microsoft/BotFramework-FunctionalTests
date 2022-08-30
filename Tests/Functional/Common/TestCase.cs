// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Builder.Tests.Functional.Common
{
    public class TestCase<TBot>
    {
        public string Channel { get; set; }

        public TBot Bot { get; set; }

        public string Script { get; set; }

        public override string ToString()
        {
            return $"{Script}, {Bot}, {Channel}";
        }
    }
}
