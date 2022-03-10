// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Xunit;

namespace IntegrationTests.Azure.Cosmos
{
    [CosmosDb(databaseId: "CosmosDbPartitionedStorageTests")]
    public class CosmosDbPartitionedStorageFixture : CosmosDbBaseFixture, IAsyncLifetime
    {
        public IStorage Storage { get; private set; }

        public new async Task InitializeAsync()
        {
            await base.InitializeAsync();

            Storage = new CosmosDbPartitionedStorage(
                new CosmosDbPartitionedStorageOptions
                {
                    AuthKey = AuthKey,
                    ContainerId = ContainerId,
                    CosmosDbEndpoint = ServiceEndpoint,
                    DatabaseId = DatabaseId,
                });
        }
    }
}
