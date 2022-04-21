// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Xunit;

namespace Microsoft.Bot.Builder.Tests.Integration.Azure.Cosmos
{
    [Trait("TestCategory", "Deprecated")]
    public class CosmosDbStorageTests : CosmosDbBaseTests, IClassFixture<CosmosDbStorageFixture>
    {
        private readonly CosmosDbStorageFixture _cosmosDbFixture;

        public CosmosDbStorageTests(CosmosDbStorageFixture cosmosDbFixture)
        {
            _cosmosDbFixture = cosmosDbFixture;
        }

        [Fact]
        public Task CreateItem()
        {
            return CreateItemTest(_cosmosDbFixture.Storage);
        }

        [Fact]
        public Task CreateItemWithSpecialCharacters()
        {
            return CreateItemWithSpecialCharactersTest(_cosmosDbFixture.Storage);
        }

        [Fact]
        public Task ReadUnknownItem()
        {
            return ReadUnknownItemTest(_cosmosDbFixture.Storage);
        }

        [Fact]
        public Task UpdateItem()
        {
            return UpdateItemTest(_cosmosDbFixture.Storage);
        }

        [Fact]
        public Task DeleteItem()
        {
            return DeleteItemTest(_cosmosDbFixture.Storage);
        }

        [Fact]
        public async Task PartitionedContainer_CreateItem()
        {
            await _cosmosDbFixture.CreateStoragePartitionedContainer("document/city");
            var storage = _cosmosDbFixture.GetStoragePartitionedContainer(Sample.City);

            var dict = new Dictionary<string, object>
            {
                { "createPartitionedItem", Sample }
            };

            await storage.WriteAsync(dict);
            var createdItems = await storage.ReadAsync<CosmosDbStorageItem>(dict.Keys.ToArray());

            Assert.Equal(Sample.City, createdItems.First().Value.City);
        }

        [Fact]
        public async Task PartitionedContainer_ReadUnknownItem()
        {
            await _cosmosDbFixture.CreateStoragePartitionedContainer("document/city");
            var storage = _cosmosDbFixture.GetStoragePartitionedContainer(Sample.City);

            var result = await storage.ReadAsync(new[] { "unknown" });

            Assert.Empty(result);
        }

        [Fact]
        public async Task PartitionedContainer_UpdateItem()
        {
            await _cosmosDbFixture.CreateStoragePartitionedContainer("document/city");
            var storage = _cosmosDbFixture.GetStoragePartitionedContainer(Sample.City);

            var dict = new Dictionary<string, object>
            {
                { "updatePartitionedItem", Sample }
            };

            await storage.WriteAsync(dict);

            var createdItems = await storage.ReadAsync(dict.Keys.ToArray());
            var created = createdItems.First().Value as CosmosDbStorageItem;

            // Update store item
            created.MessageList = new string[] { "new message" };

            await storage.WriteAsync(createdItems);

            var updatedItems = await storage.ReadAsync(dict.Keys.ToArray());
            var updated = updatedItems.First().Value as CosmosDbStorageItem;

            Assert.NotEqual(created.ETag, updated.ETag);
            Assert.Single(updated.MessageList);
            Assert.Equal(created.MessageList, updated.MessageList);
        }

        [Fact]
        public async Task PartitionedContainer_DeleteItem()
        {
            await _cosmosDbFixture.CreateStoragePartitionedContainer("document/city");
            var storage = _cosmosDbFixture.GetStoragePartitionedContainer(Sample.City);

            var dict = new Dictionary<string, object>
            {
                { "deletePartitionedItem", Sample }
            };

            await storage.WriteAsync(dict);

            // Delete store item
            await storage.DeleteAsync(dict.Keys.ToArray());
            var deleted = await storage.ReadAsync(dict.Keys.ToArray());

            Assert.Empty(deleted);
        }
    }
}
