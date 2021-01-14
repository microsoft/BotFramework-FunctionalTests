// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
        public TestClientFactory(ClientType client, DirectLinetTestClientOptions options)
        {
            switch (client)
            {
                case ClientType.DirectLine:
                    _testClientBase = new DirectLineTestClient(options);
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
                    _testClientBase = new DirectLineTestClient(options);
                    break;
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
