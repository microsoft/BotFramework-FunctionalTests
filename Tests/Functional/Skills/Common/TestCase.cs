// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Builder.Tests.Functional.Skills.Common
{
    public class TestCase
    {
        public string Description { get; set; }

        public string ChannelId { get; set; }

        public string DeliveryMode { get; set; }

        public HostBot HostBot { get; set; }

        public string TargetSkill { get; set; }

        public string Script { get; set; }
    }
}
