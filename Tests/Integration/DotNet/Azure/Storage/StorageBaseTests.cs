// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Adapters;
using Xunit;

namespace Microsoft.Bot.Builder.Tests.Integration.Azure.Storage
{
    public abstract class StorageBaseTests
    {
        public StorageBaseTests()
        {
            Sample = new StorageItem() { MessageList = new string[] { "hi", "how are u" }, City = "Contoso" };
        }

        public StorageItem Sample { get; private set; }

        public IDictionary<StorageCase, IStorage> Storages { get; private set; }

        protected void UseStorages(IDictionary<StorageCase, IStorage> storages)
        {
            Storages = storages;
        }

        [Fact]
        protected virtual async Task ReadUnknownItem()
        {
            var storage = Storages[StorageCase.Default];
            var result = await storage.ReadAsync(new[] { "unknown" });

            Assert.Empty(result);
        }

        [Fact]
        protected virtual async Task DeleteUnknownItem()
        {
            var storage = Storages[StorageCase.Default];
            await storage.DeleteAsync(new[] { "unknown" });
        }

        [Fact]

        protected virtual async Task CreateItem()
        {
            var storage = Storages[StorageCase.Default];
            var dict = new Dictionary<string, object>
            {
                { "createItem", Sample },
            };

            await storage.WriteAsync(dict);

            var createdItems = await storage.ReadAsync<StorageItem>(dict.Keys.ToArray());
            var created = createdItems.FirstOrDefault().Value;

            Assert.Single(createdItems);
            Assert.NotNull(created);
            Assert.NotNull(created.ETag);
            Assert.Equal(Sample.City, created.City);
            Assert.Equal(Sample.MessageList, created.MessageList);
        }

        [Fact]
        protected virtual async Task CreateItemWithSpecialCharacters()
        {
            var storage = Storages[StorageCase.Default];
            const string key = "!@#$%^&*()~/\\><,.?';\"`~";
            var dict = new Dictionary<string, object>
            {
                { key, Sample },
            };

            await storage.WriteAsync(dict);

            var createdItems = await storage.ReadAsync<StorageItem>(dict.Keys.ToArray());
            var created = createdItems.FirstOrDefault().Value;

            Assert.Single(createdItems);
            Assert.NotNull(created);
            Assert.NotNull(created.ETag);
            Assert.Equal(Sample.City, created.City);
            Assert.Equal(Sample.MessageList, created.MessageList);
        }

        [Fact]
        protected virtual async Task UpdateItem()
        {
            var storage = Storages[StorageCase.Default];
            var dict = new Dictionary<string, object>
            {
                { "updateItem", Sample },
            };

            await storage.WriteAsync(dict);

            var createdItems = await storage.ReadAsync(dict.Keys.ToArray());
            var created = createdItems.FirstOrDefault().Value as StorageItem;

            // Update store item
            created.MessageList = new string[] { "new message" };

            await storage.WriteAsync(createdItems);

            var updatedItems = await storage.ReadAsync(dict.Keys.ToArray());
            var updated = updatedItems.FirstOrDefault().Value as StorageItem;

            Assert.NotEqual(created.ETag, updated.ETag);
            Assert.Single(updated.MessageList);
            Assert.Equal(created.MessageList, updated.MessageList);
        }

        [Fact]
        protected virtual async Task DeleteItem()
        {
            var storage = Storages[StorageCase.Default];
            var dict = new Dictionary<string, object>
            {
                { "deleteItem", Sample },
            };

            await storage.WriteAsync(dict);

            var createdItems = await storage.ReadAsync<StorageItem>(dict.Keys.ToArray());
            var created = createdItems.FirstOrDefault().Value;

            // Delete store item
            await storage.DeleteAsync(dict.Keys.ToArray());

            var deleted = await storage.ReadAsync(dict.Keys.ToArray());

            Assert.NotNull(created);
            Assert.Empty(deleted);
        }

        [Theory]
        [InlineData(StorageCase.Default)]
        [InlineData(StorageCase.TypeNameHandlingNone)]
        protected virtual async Task CreateItemFromConversationState(StorageCase storageCase)
        {
            var storage = Storages[storageCase];
            var conversationState = new ConversationState(storage);
            var prop = conversationState.CreateProperty<StorageItem>(nameof(CreateItemFromConversationState));

            var adapter = new TestAdapter();
            var activity = adapter.MakeActivity();
            activity.ChannelId = nameof(CreateItemFromConversationState);

            // Created
            var createdContext = new TurnContext(adapter, activity);
            var created = await prop.GetAsync(createdContext, () => Sample);
            await conversationState.SaveChangesAsync(createdContext, force: true);

            // Read
            var resultContext = new TurnContext(adapter, activity);
            var result = await prop.GetAsync(resultContext);

            Assert.NotNull(result);
            Assert.Equal(created.City, result.City);
            Assert.Equal(created.MessageList, result.MessageList);
        }

        [Fact]
        protected virtual async Task UpdateItemFromConversationState()
        {
            var storage = Storages[StorageCase.Default];
            var conversationState = new ConversationState(storage);
            var prop = conversationState.CreateProperty<StorageItem>(nameof(UpdateItemFromConversationState));

            var adapter = new TestAdapter();
            var activity = adapter.MakeActivity();
            activity.ChannelId = nameof(UpdateItemFromConversationState);

            // Create
            var createdContext = new TurnContext(adapter, activity);
            var created = await prop.GetAsync(createdContext, () => Sample);
            await conversationState.SaveChangesAsync(createdContext, force: true);

            // Update
            var updatedContext = new TurnContext(adapter, activity);
            var updated = await prop.GetAsync(updatedContext);
            updated.MessageList = new string[] { "new message" };
            await conversationState.SaveChangesAsync(updatedContext, force: true);

            // Read
            var resultContext = new TurnContext(adapter, activity);
            var result = await prop.GetAsync(resultContext);

            Assert.NotNull(result);
            Assert.NotEqual(created.MessageList, result.MessageList);
            Assert.Equal(updated.MessageList, result.MessageList);
        }

        [Fact]
        protected virtual async Task DeleteItemFromConversationState()
        {
            var storage = Storages[StorageCase.Default];
            var conversationState = new ConversationState(storage);
            var prop = conversationState.CreateProperty<StorageItem>(nameof(DeleteItemFromConversationState));

            var adapter = new TestAdapter();
            var activity = adapter.MakeActivity();
            activity.ChannelId = nameof(DeleteItemFromConversationState);

            // Create
            var createdContext = new TurnContext(adapter, activity);
            var created = await prop.GetAsync(createdContext, () => Sample);

            // Delete
            await prop.DeleteAsync(createdContext);
            await conversationState.SaveChangesAsync(createdContext, force: true);

            // Read
            var resultContext = new TurnContext(adapter, activity);
            var result = await prop.GetAsync(resultContext);

            Assert.NotNull(created);
            Assert.Null(result);
        }

        [Theory]
        [InlineData(StorageCase.Default)]
        [InlineData(StorageCase.TypeNameHandlingNone)]
        protected virtual async Task StatePersistsThroughMultiTurn(StorageCase storageCase)
        {
            var storage = Storages[storageCase];
            var userState = new UserState(storage);
            var prop = userState.CreateProperty<StorageItem>("item");
            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(userState));

            var messageList = new string[] { "new message" };

            await new TestFlow(
                adapter,
                async (context, cancellationToken) =>
                {
                    var state = await prop.GetAsync(context, () => Sample);
                    Assert.NotNull(state);
                    switch (context.Activity.Text)
                    {
                        case "set value":
                            state.MessageList = messageList;
                            await context.SendActivityAsync("value saved");
                            break;
                        case "get value":
                            var message = string.Join(",", state.MessageList);
                            await context.SendActivityAsync(message);
                            break;
                    }
                })
                .Test("set value", "value saved")
                .Test("get value", string.Join(",", messageList))
                .StartTestAsync();
        }
    }
}
