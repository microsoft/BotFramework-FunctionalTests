// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;

namespace TranscriptTestRunner.TestClients
{
    /// <summary>
    /// DirectLine session information definition.
    /// </summary>
    public class DirectLineSessionInfo
    {
        /// <summary>
        /// Gets or sets the DirectLine session ID.
        /// </summary>
        /// <value>
        /// The DirectLine session ID.
        /// </value>
        public string SessionId { get; set; }

        /// <summary>
        /// Gets or sets the DirectLine session cookie.
        /// </summary>
        /// <value>
        /// The DirectLine session cookie.
        /// </value>
        public Cookie Cookie { get; set; }
    }
}
