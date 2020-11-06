// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace TranscriptTestRunner.TestClients
{
    /// <summary>
    /// DirectLine token definition.
    /// </summary>
    public class DirectLineToken
    {
        /// <summary>
        /// Gets or sets the DirectLine token string.
        /// </summary>
        /// <value>
        /// The DirectLine token string.
        /// </value>
        [JsonProperty("token")]
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the DirectLine conversation ID.
        /// </summary>
        /// <value>
        /// The DirectLine conversation ID.
        /// </value>
        [JsonProperty("conversationId")]
        public string ConversationId { get; set; }
    }
}
