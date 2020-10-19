// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace SkillFunctionalTests.Bot
{
    public class DirectLineSession
    {
        [JsonProperty("sessionId")]
        public string SessionId { get; set; }
    }
}
