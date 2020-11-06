// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
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
        public TestClientFactory(ClientType client)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            switch (client)
            {
                case ClientType.DirectLine:
                    _testClientBase = new DirectLineTestClient(configuration);
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
                    _testClientBase = new DirectLineTestClient(configuration);
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
