// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Xunit;

namespace IntegrationTests.Azure.Cosmos
{
    public class CosmosDbBaseTests
    {
        public CosmosDbBaseTests()
        {
            Sample = new CosmosDbStorageItem() { MessageList = new string[] { "hi", "how are u" }, City = "Contoso" };
        }

        public CosmosDbStorageItem Sample { get; private set; }

        protected async Task ReadUnknownItemTest(IStorage storage)
        {
            var result = await storage.ReadAsync(new[] { "unknown" });

            Assert.Empty(result);
        }

        protected async Task DeleteUnknownItemTest(IStorage storage)
        {
            await storage.DeleteAsync(new[] { "unknown" });
        }

        protected async Task CreateItemTest(IStorage storage)
        {
            var dict = new Dictionary<string, object>
            {
                { "createItem", Sample },
            };

            await storage.WriteAsync(dict);

            var createdItems = await storage.ReadAsync<CosmosDbStorageItem>(dict.Keys.ToArray());
            var created = createdItems.FirstOrDefault().Value;

            Assert.Single(createdItems);
            Assert.NotNull(created);
            Assert.NotNull(created.ETag);
            Assert.Equal(Sample.City, created.City);
            Assert.Equal(Sample.MessageList, created.MessageList);
        }

        protected async Task CreateItemWithSpecialCharactersTest(IStorage storage)
        {
            var key = "!@#$%^&*()~/\\><,.?';\"`~";
            var dict = new Dictionary<string, object>
            {
                { key, Sample },
            };

            await storage.WriteAsync(dict);

            var createdItems = await storage.ReadAsync<CosmosDbStorageItem>(dict.Keys.ToArray());
            var created = createdItems.FirstOrDefault().Value;

            Assert.Single(createdItems);
            Assert.NotNull(created);
            Assert.NotNull(created.ETag);
            Assert.Equal(Sample.City, created.City);
            Assert.Equal(Sample.MessageList, created.MessageList);
        }

        protected async Task UpdateItemTest(IStorage storage)
        {
            var dict = new Dictionary<string, object>
            {
                { "updateItem", Sample },
            };

            await storage.WriteAsync(dict);

            var createdItems = await storage.ReadAsync(dict.Keys.ToArray());
            var created = createdItems.FirstOrDefault().Value as CosmosDbStorageItem;

            // Update store item
            created.MessageList = new string[] { "new message" };

            await storage.WriteAsync(createdItems);

            var updatedItems = await storage.ReadAsync(dict.Keys.ToArray());
            var updated = updatedItems.FirstOrDefault().Value as CosmosDbStorageItem;

            Assert.NotEqual(created.ETag, updated.ETag);
            Assert.Single(updated.MessageList);
            Assert.Equal(created.MessageList, updated.MessageList);
        }

        protected async Task DeleteItemTest(IStorage storage)
        {
            var dict = new Dictionary<string, object>
            {
                { "deleteItem", Sample },
            };

            await storage.WriteAsync(dict);

            var createdItems = await storage.ReadAsync<CosmosDbStorageItem>(dict.Keys.ToArray());
            var created = createdItems.FirstOrDefault().Value;

            // Delete store item
            await storage.DeleteAsync(dict.Keys.ToArray());

            var deleted = await storage.ReadAsync(dict.Keys.ToArray());

            Assert.NotNull(created);
            Assert.Empty(deleted);
        }
    }
}
