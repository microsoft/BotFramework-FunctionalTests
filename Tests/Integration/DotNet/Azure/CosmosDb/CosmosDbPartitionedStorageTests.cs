// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using Xunit;

namespace IntegrationTests.Azure.CosmosDb
{
    [Trait("TestCategory", "Storage")]
    [Trait("TestCategory", "CosmosDb Partitioned")]
    public class CosmosDbPartitionedStorageTests : CosmosDbBaseTests, IClassFixture<CosmosDbPartitionedStorageFixture>
    {
        private readonly CosmosDbPartitionedStorageFixture _cosmosDbFixture;

        public CosmosDbPartitionedStorageTests(CosmosDbPartitionedStorageFixture cosmosDbFixture)
        {
            _cosmosDbFixture = cosmosDbFixture;
        }

        [Fact]
        public Task CreateItem()
        {
            return CreateItemTest(_cosmosDbFixture.Storage);
        }

        [Fact]
        public Task UpdateItem()
        {
            return UpdateItemTest(_cosmosDbFixture.Storage);
        }

        [Fact]
        public Task ReadUnknownItem()
        {
            return ReadUnknownItemTest(_cosmosDbFixture.Storage);
        }

        [Fact]
        public Task DeleteItem()
        {
            return DeleteItemTest(_cosmosDbFixture.Storage);
        }

        [Fact]
        public Task CreateItemWithSpecialCharacters()
        {
            return CreateItemWithSpecialCharactersTest(_cosmosDbFixture.Storage);
        }

        [Fact]
        public async Task CreateItemWithNestingLimit()
        {
            async Task TestNestAsync(int depth)
            {
                // This creates nested data with both objects and arrays
                static JToken CreateNestedData(int count, bool isArray = false)
                    => count > 0
                        ? (isArray
                            ? new JArray { CreateNestedData(count - 1, false) } as JToken
                            : new JObject { new JProperty("data", CreateNestedData(count - 1, true)) })
                        : null;

                var dict = new Dictionary<string, object>
                {
                    { "nestingLimit", CreateNestedData(depth) },
                };

                await _cosmosDbFixture.Storage.WriteAsync(dict);
            }

            // Should not throw
            await TestNestAsync(127);

            try
            {
                // Should either not throw or throw a special exception
                await TestNestAsync(128);
            }
            catch (InvalidOperationException ex)
            {
                // If the nesting limit is changed on the Cosmos side
                // then this assertion won't be reached, which is okay
                Assert.Contains("recursion", ex.Message);
            }
        }

        [Fact]
        public async Task CreateItemWithDialogsNestingLimit()
        {
            async Task TestDialogNestAsync(int dialogDepth)
            {
                Dialog CreateNestedDialog(int depth) => new ComponentDialog(nameof(ComponentDialog))
                    .AddDialog(depth > 0
                        ? CreateNestedDialog(depth - 1)
                        : new WaterfallDialog(
                            nameof(WaterfallDialog),
                            new List<WaterfallStep>
                            {
                                (stepContext, ct) => Task.FromResult(Dialog.EndOfTurn)
                            }));

                var dialog = CreateNestedDialog(dialogDepth);

                var convoState = new ConversationState(_cosmosDbFixture.Storage);

                var adapter = new TestAdapter(TestAdapter.CreateConversation("nestingTest"))
                    .Use(new AutoSaveStateMiddleware(convoState));

                var dialogState = convoState.CreateProperty<DialogState>("dialogStateForNestingTest");

                await new TestFlow(adapter, async (turnContext, cancellationToken) =>
                {
                    if (turnContext.Activity.Text == "reset")
                    {
                        await dialogState.DeleteAsync(turnContext);
                    }
                    else
                    {
                        await dialog.RunAsync(turnContext, dialogState, cancellationToken);
                    }
                })
                    .Send("reset")
                    .Send("hello")
                    .StartTestAsync();
            }

            // Should not throw
            await TestDialogNestAsync(23);

            try
            {
                // Should either not throw or throw a special exception
                await TestDialogNestAsync(24);
            }
            catch (InvalidOperationException ex)
            {
                // If the nesting limit is changed on the Cosmos side
                // then this assertion won't be reached, which is okay
                Assert.Contains("dialogs", ex.Message);
            }
        }
    }
}
