// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace TranscriptTestRunner
{
    public class TestScriptItem
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("assertions")] 
        public List<string> Assertions { get; } = new List<string>();

        public bool ShouldSerializeAssertions()
        {
            return Assertions.Count > 0;
        }
    }
}
