// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Tests.Integration.Azure.Storage;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Bot.Builder.Tests.Integration.Azure.Cosmos
{
    [CosmosDb(databaseId: "CosmosDbStorageTests")]
    public class CosmosDbStorageFixture : CosmosDbBaseFixture, IAsyncLifetime
    {
        public CosmosDbStorageFixture()
        {
            PartitionedContainerId = "CosmosPartitionedContainer";
        }

        public string PartitionedContainerId { get; private set; }

        public IDictionary<StorageCase, IStorage> Storages { get; private set; }

        public new async Task InitializeAsync()
        {
            await base.InitializeAsync();

            var options = new CosmosDbStorageOptions
            {
                AuthKey = AuthKey,
                CollectionId = ContainerId,
                CosmosDBEndpoint = new Uri(ServiceEndpoint),
                DatabaseId = DatabaseId,
            };

            Storages = new Dictionary<StorageCase, IStorage>
            {
                { StorageCase.Default, new CosmosDbStorage(options) },
                { StorageCase.TypeNameHandlingNone, new CosmosDbStorage(options, new JsonSerializer() { TypeNameHandling = TypeNameHandling.None }) }
            };
        }

        public IStorage GetStoragePartitionedContainer(string partitionKey)
        {
            return new CosmosDbStorage(new CosmosDbStorageOptions()
            {
                PartitionKey = partitionKey,
                AuthKey = AuthKey,
                CollectionId = PartitionedContainerId,
                CosmosDBEndpoint = new Uri(ServiceEndpoint),
                DatabaseId = DatabaseId,
            });
        }

        public async Task CreateStoragePartitionedContainer(string partitionKeyPath)
        {
            using var client = new DocumentClient(new Uri(ServiceEndpoint), AuthKey);
            Database database = await client.CreateDatabaseIfNotExistsAsync(new Database { Id = DatabaseId });
            var partitionKeyDefinition = new PartitionKeyDefinition { Paths = new Collection<string> { $"/{partitionKeyPath}" } };
            var collectionDefinition = new DocumentCollection { Id = PartitionedContainerId, PartitionKey = partitionKeyDefinition };

            await client.CreateDocumentCollectionIfNotExistsAsync(database.SelfLink, collectionDefinition);
        }
    }
}
