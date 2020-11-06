// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace TranscriptTestRunner.TestClients
{
    /// <summary>
    /// DirectLine session definition.
    /// </summary>
    public class DirectLineSession
    {
        /// <summary>
        /// Gets or sets the DirectLine session ID.
        /// </summary>
        /// <value>
        /// The DirectLine session ID.
        /// </value>
        [JsonProperty("sessionId")]
        public string SessionId { get; set; }
    }
}
