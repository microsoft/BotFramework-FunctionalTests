// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace TranscriptTestRunner.TestClients
{
    /// <summary>
    /// Class with the options needed to run a test against a TesClient.
    /// </summary>
    public class TestClientOptions
    {
        /// <summary>
        /// Gets or sets the Id of the host bot.
        /// </summary>
        /// <value>The Id of the host bot.</value>
        public string BotId { get; set; }

        /// <summary>
        /// Gets or sets the secret of the DirectLine connection.
        /// </summary>
        /// <value>The secret of the DirectLine connection.</value>
        public string DirectLineClientSecret { get; set; }
    }
}
