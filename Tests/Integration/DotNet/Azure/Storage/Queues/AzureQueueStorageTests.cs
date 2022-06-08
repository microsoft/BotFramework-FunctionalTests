// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Azure.Queues;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Bot.Builder.Tests.Integration.Azure.Storage.Queues
{
    public class AzureQueueStorageTests : IClassFixture<AzureQueueBaseFixture>, IAsyncLifetime
    {
        private readonly AzureQueueBaseFixture _azureQueueFixture;
        private readonly ITestOutputHelper _outputHandler;

        private readonly Activity _welcomeSample = new Activity { Type = ActivityTypes.Message, Text = "Welcome!" };
        private readonly Activity _greetingsSample = new Activity { Type = ActivityTypes.Message, Text = "Greetings!" };

        public AzureQueueStorageTests(ITestOutputHelper outputHandler, AzureQueueBaseFixture azureQueueFixture)
        {
            _azureQueueFixture = azureQueueFixture;
            _outputHandler = outputHandler;
        }

        private QueueClient Client { get; set; }

        private QueueStorage Storage { get; set; }

        public async Task InitializeAsync()
        {
            // Use each test method name to create a different container.
            var testMember = _outputHandler.GetType().GetField("test", BindingFlags.Instance | BindingFlags.NonPublic);
            var test = (ITest)testMember.GetValue(_outputHandler);
            var containerName = test.TestCase.TestMethod.Method.Name.ToLower();

            Storage = new AzureQueueStorage(_azureQueueFixture.ConnectionString, containerName);
            Client = new QueueClient(_azureQueueFixture.ConnectionString, containerName);

            await Client.CreateIfNotExistsAsync();
            await Client.ClearMessagesAsync();
        }

        public async Task DisposeAsync()
        {
            await Client?.DeleteIfExistsAsync();
        }

        [Fact]
        public async Task QueueActivity()
        {
            await Storage.QueueActivityAsync(_welcomeSample);

            var messages = await Client.ReceiveMessagesAsync();
            var activity = DeserializeQueueMessage(messages.Value.First());

            Assert.Equal(ActivityTypes.Message, activity.Type);
            Assert.Equal(_welcomeSample.Text, activity.Text);
        }

        [Fact]
        public async Task QueueMultipleActivities()
        {
            await Storage.QueueActivityAsync(_welcomeSample);
            await Storage.QueueActivityAsync(_greetingsSample);

            var initialMessages = (await Client.PeekMessagesAsync(3)).Value;

            var messages = await Client.ReceiveMessagesAsync();
            var welcomeActivity = DeserializeQueueMessage(messages.Value.First());

            messages = await Client.ReceiveMessagesAsync();
            var greetingsActivity = DeserializeQueueMessage(messages.Value.First());

            var finalMessages = (await Client.PeekMessagesAsync()).Value;

            Assert.Equal(2, initialMessages.Length);
            Assert.Empty(finalMessages);

            Assert.Equal(ActivityTypes.Message, welcomeActivity.Type);
            Assert.Equal(_welcomeSample.Text, welcomeActivity.Text);

            Assert.Equal(ActivityTypes.Message, greetingsActivity.Type);
            Assert.Equal(_greetingsSample.Text, greetingsActivity.Text);
        }

        [Fact]
        public async Task QueueActivityWithExpiration()
        {
            await Storage.QueueActivityAsync(_welcomeSample, TimeSpan.Zero, TimeSpan.FromSeconds(1));

            await Task.Delay(1000);

            var messages = await Client.ReceiveMessagesAsync();

            Assert.Empty(messages.Value);
        }

        [Fact]
        public async Task QueueActivityWithVisibility()
        {
            await Storage.QueueActivityAsync(_welcomeSample, TimeSpan.FromSeconds(1));

            var messages = await Client.ReceiveMessagesAsync();

            Assert.Empty(messages.Value);
        }

        [Fact]
        public async Task QueueContinueConversationLater()
        {
            var cr = TestAdapter.CreateConversation(nameof(QueueContinueConversationLater));
            var adapter = new TestAdapter(cr)
                   .UseStorage(new MemoryStorage())
                   .UseBotState(new ConversationState(new MemoryStorage()), new UserState(new MemoryStorage()));

            var dm = new DialogManager(new ContinueConversationLater()
            {
                Date = "=addSeconds(utcNow(), 1)",
                Value = "foo"
            });

            dm.InitialTurnState.Set(Storage);

            await new TestFlow((TestAdapter)adapter, dm.OnTurnAsync)
                .Send("hi")
                .StartTestAsync();

            var messages = await Client.ReceiveMessagesAsync();
            var activity = DeserializeQueueMessage(messages.Value.First());

            var cr2 = activity.GetConversationReference();
            cr.ActivityId = null;
            cr2.ActivityId = null;

            Assert.Equal(ActivityTypes.Event, activity.Type);
            Assert.Equal(ActivityEventNames.ContinueConversation, activity.Name);
            Assert.Equal("foo", activity.Value);
            Assert.NotNull(activity.RelatesTo);
            Assert.Equal(JsonConvert.SerializeObject(cr), JsonConvert.SerializeObject(cr2));
        }

        private static Activity DeserializeQueueMessage(QueueMessage message)
        {
            var messageJson = Encoding.UTF8.GetString(Convert.FromBase64String(message.MessageText));
            var activity = JsonConvert.DeserializeObject<Activity>(messageJson);
            return activity;
        }
    }
}
