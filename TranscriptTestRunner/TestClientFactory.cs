// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using TranscriptTestRunner.TestClients;

namespace TranscriptTestRunner
{
    public class TestClientFactory
    {
        private readonly TestClientBase _testClientBase;

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

        public TestClientBase GetTestClient()
        {
            return _testClientBase;
        }
    }
}
