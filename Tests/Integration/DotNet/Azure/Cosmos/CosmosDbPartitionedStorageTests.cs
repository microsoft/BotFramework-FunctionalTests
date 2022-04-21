// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Tests.Integration.Azure.Storage;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Bot.Builder.Tests.Integration.Azure.Cosmos
{
    public class CosmosDbPartitionedStorageTests : StorageBaseTests, IClassFixture<CosmosDbPartitionedStorageFixture>
    {
        public CosmosDbPartitionedStorageTests(CosmosDbPartitionedStorageFixture cosmosDbFixture)
        {
            UseStorages(cosmosDbFixture.Storages);
        }

        [Fact]
        public async Task CreateItemWithNestingLimit()
        {
            var storage = Storages[StorageCase.Default];
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

                await storage.WriteAsync(dict);
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
            var storage = Storages[StorageCase.Default];
            async Task TestDialogNestAsync(int dialogDepth)
            {
                static Dialog CreateNestedDialog(int depth) => new ComponentDialog(nameof(ComponentDialog))
                    .AddDialog(depth > 0
                        ? CreateNestedDialog(depth - 1)
                        : new WaterfallDialog(
                            nameof(WaterfallDialog),
                            new List<WaterfallStep>
                            {
                                (stepContext, ct) => Task.FromResult(Dialog.EndOfTurn)
                            }));

                var dialog = CreateNestedDialog(dialogDepth);

                var convoState = new ConversationState(storage);

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
