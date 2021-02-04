// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using TranscriptTestRunner.TestClients;

namespace TranscriptTestRunner
{
    /// <summary>
    /// Factory class to create instances of <see cref="TestClientBase"/>.
    /// </summary>
    public class TestClientFactory
    {
        private readonly TestClientBase _testClientBase;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestClientFactory"/> class.
        /// </summary>
        /// <param name="client">The type of client to create.</param>
        /// <param name="options">The options to create the client.</param>
        /// <param name="logger">An optional <see cref="ILogger"/> instance.</param>
        public TestClientFactory(ClientType client, DirectLineTestClientOptions options, ILogger logger)
        {
            switch (client)
            {
                case ClientType.DirectLine:
                    _testClientBase = new DirectLineTestClient(options, logger);
                    break;
                case ClientType.Emulator:
                    break;
                case ClientType.Teams:
                    break;
                case ClientType.Facebook:
                    break;
                case ClientType.Slack:
                    break;
                default:
                    throw new InvalidEnumArgumentException($"Invalid client type ({client})");
            }
        }

        /// <summary>
        /// Gets the test client.
        /// </summary>
        /// <returns>The test client.</returns>
        public TestClientBase GetTestClient()
        {
            return _testClientBase;
        }
    }
}
