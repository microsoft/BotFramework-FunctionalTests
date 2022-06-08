// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Bot.Builder.Tests.Integration.Azure.Storage.Blobs
{
    public abstract class BlobsTranscriptStoreBaseTests
    {
        private readonly string testName;
        private readonly string specialCharacters = "!@#$%^&*()~/\\><,.?';\"`~_}{][^";

        public BlobsTranscriptStoreBaseTests(ITestOutputHelper outputHandler)
        {
            var testMember = outputHandler.GetType().GetField("test", BindingFlags.Instance | BindingFlags.NonPublic);
            var test = (ITest)testMember.GetValue(outputHandler);
            testName = test.TestCase.TestMethod.Method.Name.ToLower();
        }

        public IDictionary<StorageCase, ITranscriptStore> Storages { get; private set; }

        protected void UseStorages(IDictionary<StorageCase, ITranscriptStore> storages)
        {
            Storages = storages;
        }

        [Fact]
        protected virtual async Task ListEmptyTranscripts()
        {
            var storage = Storages[StorageCase.Default];
            var transcripts = await storage.ListTranscriptsAsync("unknown");

            Assert.Empty(transcripts.Items);
        }

        [Fact]
        protected virtual async Task ListEmptyActivities()
        {
            var storage = Storages[StorageCase.Default];
            var activities = await storage.GetTranscriptActivitiesAsync("unknown", "unknown");

            Assert.Empty(activities.Items);
        }

        [Fact]
        protected virtual async Task CreateActivity()
        {
            var storage = Storages[StorageCase.Default];
            var activity = GetActivity();
            await storage.LogActivityAsync(activity);

            var page = await storage.GetTranscriptActivitiesAsync(activity.ChannelId, activity.Conversation.Id);
            var activities = page.Items.Cast<Activity>().ToList();
            var createdActivity = activities.FirstOrDefault();

            Assert.Single(activities);
            Assert.Equal(activity.Id, createdActivity.Id);
            Assert.Equal(activity.Text, createdActivity.Text);
            Assert.Equal(activity.Type, createdActivity.Type);
        }

        [Fact]
        protected virtual async Task UpdateActivity()
        {
            var storage = Storages[StorageCase.Default];
            var activity = GetActivity();
            await storage.LogActivityAsync(activity);

            var page = await storage.GetTranscriptActivitiesAsync(activity.ChannelId, activity.Conversation.Id);
            var activities = page.Items.Cast<Activity>().ToList();
            var createdActivity = activities.FirstOrDefault();

            var activityToUpdate = CloneActivity(activity);
            activityToUpdate.Type = ActivityTypes.MessageUpdate;
            activityToUpdate.Text = "updated";
            await storage.LogActivityAsync(activityToUpdate);

            page = await storage.GetTranscriptActivitiesAsync(activityToUpdate.ChannelId, activityToUpdate.Conversation.Id);
            var updatedActivities = page.Items.Cast<Activity>().ToList();
            var updatedActivity = updatedActivities.FirstOrDefault();

            Assert.Single(activities);
            Assert.Equal(activity.Id, createdActivity.Id);
            Assert.Equal(activity.Text, createdActivity.Text);
            Assert.Equal(activity.Type, createdActivity.Type);

            Assert.Single(updatedActivities);
            Assert.Equal(createdActivity.Id, updatedActivity.Id);
            Assert.NotEqual(createdActivity.Text, updatedActivity.Text);
            Assert.Equal("updated", updatedActivity.Text);
            Assert.Equal(ActivityTypes.Message, updatedActivity.Type);
        }

        [Fact]
        protected virtual async Task DeleteActivity()
        {
            var storage = Storages[StorageCase.Default];
            var activity = GetActivity();
            await storage.LogActivityAsync(activity);

            await storage.DeleteTranscriptAsync(activity.ChannelId, activity.Conversation.Id);
            var page = await storage.GetTranscriptActivitiesAsync(activity.ChannelId, activity.Conversation.Id);
            var activities = page.Items.Cast<Activity>().ToList();

            Assert.Empty(activities);
        }

        [Fact]
        protected virtual async Task TombstonedActivity()
        {
            var storage = Storages[StorageCase.Default];
            var activity = GetActivity();
            await storage.LogActivityAsync(activity);

            var page = await storage.GetTranscriptActivitiesAsync(activity.ChannelId, activity.Conversation.Id);
            var activities = page.Items.Cast<Activity>().ToList();
            var createdActivity = activities.FirstOrDefault();

            var activityToUpdate = CloneActivity(activity);
            activityToUpdate.Type = ActivityTypes.MessageDelete;
            await storage.LogActivityAsync(activityToUpdate);

            page = await storage.GetTranscriptActivitiesAsync(activityToUpdate.ChannelId, activityToUpdate.Conversation.Id);
            var updatedActivities = page.Items.Cast<Activity>().ToList();
            var updatedActivity = updatedActivities.FirstOrDefault();

            Assert.Single(updatedActivities);
            Assert.Equal(createdActivity.Id, updatedActivity.Id);
            Assert.Equal("deleted", updatedActivity.From.Id);
            Assert.Equal(ActivityTypes.MessageDelete, updatedActivity.Type);
        }

        [Fact]
        protected virtual async Task CreateActivityWithSpecialCharacters()
        {
            var storage = Storages[StorageCase.Default];
            var activity = GetActivity(id: specialCharacters, conversationId: specialCharacters);
            await storage.LogActivityAsync(activity);

            var page = await storage.GetTranscriptActivitiesAsync(activity.ChannelId, activity.Conversation.Id);
            var activities = page.Items.Cast<Activity>().ToList();
            var createdActivity = activities.FirstOrDefault();

            Assert.Single(activities);
            Assert.Equal(activity.Id, createdActivity.Id);
            Assert.Equal(activity.Text, createdActivity.Text);
            Assert.Equal(activity.Type, createdActivity.Type);
        }

        [Fact]
        protected virtual async Task DeleteActivityWithSpecialCharacters()
        {
            var storage = Storages[StorageCase.Default];
            var activity = GetActivity(id: specialCharacters, conversationId: specialCharacters);
            await storage.LogActivityAsync(activity);

            await storage.DeleteTranscriptAsync(activity.ChannelId, activity.Conversation.Id);
            var page = await storage.GetTranscriptActivitiesAsync(activity.ChannelId, activity.Conversation.Id);
            var activities = page.Items.Cast<Activity>().ToList();

            Assert.Empty(activities);
        }

        [Fact]
        protected virtual async Task CreateActivityWithPagedResult()
        {
            var storage = Storages[StorageCase.Default];
            var activities = new List<Activity>();

            for (var i = 0; i < 30; i++)
            {
                var act = GetActivity(id: i.ToString());
                await storage.LogActivityAsync(act);
                activities.Add(act);
            }

            var activity = activities.FirstOrDefault();

            var page = await storage.GetTranscriptActivitiesAsync(activity.ChannelId, activity.Conversation.Id);
            var nextPage = await storage.GetTranscriptActivitiesAsync(activity.ChannelId, activity.Conversation.Id, page.ContinuationToken);

            Assert.Equal(20, page.Items.Length);
            Assert.NotNull(page.ContinuationToken);
            Assert.True(page.ContinuationToken.Length > 0);
            Assert.Equal(10, nextPage.Items.Length);
            Assert.Null(nextPage.ContinuationToken);
        }

        [Fact]
        protected virtual async Task TranscriptLoggerMiddleware_CreateActivity()
        {
            var storage = Storages[StorageCase.Default];
            var conversation = GetConversationReference();
            var adapter = new TestAdapter(conversation)
                .Use(new TranscriptLoggerMiddleware(storage));

            await new TestFlow(adapter, async (context, cancellationToken) =>
            {
                if (context.Activity.Text == "start")
                {
                    var typingActivity = new Activity
                    {
                        Type = ActivityTypes.Typing,
                        RelatesTo = context.Activity.RelatesTo
                    };
                    await context.SendActivityAsync(typingActivity);
                }
                else
                {
                    await context.SendActivityAsync("echo:" + context.Activity.Text);
                }
            })
            .Send("start")
            .AssertReply((activity) => Assert.Equal(activity.Type, ActivityTypes.Typing))
            .Delay(300)
            .Send("hi")
            .AssertReply("echo:hi")
            .StartTestAsync();

            var page = await GetPagedResultWithRetryAsync(
                storage: storage,
                conversation: conversation,
                finishWhen: p => p.Items.Length == 4);
            var activities = page.Items.Cast<Activity>().ToList();

            Assert.Equal(4, activities.Count);
            Assert.Equal("start", activities[0].Text);
            Assert.Equal(ActivityTypes.Typing, activities[1].Type);
            Assert.Equal("hi", activities[2].Text);
            Assert.Equal("echo:hi", activities[3].Text);
            foreach (var activity in activities)
            {
                Assert.True(!string.IsNullOrWhiteSpace(activity.Id));
                Assert.True(activity.Timestamp > default(DateTimeOffset));
            }
        }

        [Fact]
        protected virtual async Task TranscriptLoggerMiddleware_UpdateActivity()
        {
            var storage = Storages[StorageCase.Default];
            var conversation = GetConversationReference();
            var adapter = new TestAdapter(conversation)
                .Use(new TranscriptLoggerMiddleware(storage));

            Activity activityToUpdate = null;
            await new TestFlow(adapter, async (context, cancellationToken) =>
            {
                if (context.Activity.Text == "update")
                {
                    activityToUpdate.Text = "updated";
                    await context.UpdateActivityAsync(activityToUpdate);
                }
                else
                {
                    var activity = context.Activity.CreateReply("started");
                    var response = await context.SendActivityAsync(activity);
                    activity.Id = response.Id;
                    activityToUpdate = CloneActivity(activity);
                }
            })
            .Send("start")
            .Delay(300)
            .Send("update")
            .AssertReply("updated")
            .StartTestAsync();

            var page = await GetPagedResultWithRetryAsync(
                storage: storage,
                conversation: conversation,
                finishWhen: p => p.Items.Length == 3 && p.Items[1].AsMessageActivity().Text == "updated");
            var activities = page.Items.Cast<Activity>().ToList();

            Assert.Equal(3, activities.Count);
            Assert.Equal("start", activities[0].Text);
            Assert.Equal("updated", activities[1].Text);
            Assert.Equal("update", activities[2].Text);
        }

        [Fact]
        protected virtual async Task TranscriptLoggerMiddleware_UpdateActivityWithDate()
        {
            var storage = Storages[StorageCase.Default];
            var conversation = GetConversationReference();
            var adapter = new TestAdapter(conversation)
                .Use(new TranscriptLoggerMiddleware(storage));
            var startTime = new DateTimeOffset(DateTime.Now);

            Activity activityToUpdate = null;
            await new TestFlow(adapter, async (context, cancellationToken) =>
            {
                if (context.Activity.Text == "update")
                {
                    activityToUpdate.Text = "updated";
                    await context.UpdateActivityAsync(activityToUpdate);
                }
                else
                {
                    var activity = context.Activity.CreateReply("started");
                    var response = await context.SendActivityAsync(activity);
                    activity.Id = response.Id;
                    activityToUpdate = CloneActivity(activity);
                }
            })
            .Send("start")
            .Delay(300)
            .Send("update")
            .AssertReply("updated")
            .StartTestAsync();

            var page = await GetPagedResultWithRetryAsync(
                storage: storage,
                conversation: conversation,
                startDate: startTime.DateTime,
                finishWhen: p => p.Items.Length == 3 && p.Items[1].AsMessageActivity().Text == "updated");
            var activities = page.Items.Cast<Activity>().ToList();

            Assert.Equal(3, activities.Count);
            Assert.Equal("start", activities[0].Text);
            Assert.Equal("updated", activities[1].Text);
            Assert.Equal("update", activities[2].Text);

            startTime = new DateTimeOffset(DateTime.Now);
            page = await storage.GetTranscriptActivitiesAsync(conversation.ChannelId, conversation.Conversation.Id, startDate: startTime);
            Assert.Empty(page.Items);
        }

        [Fact]
        protected virtual async Task TranscriptLoggerMiddleware_MissingUpdateActivity()
        {
            var storage = Storages[StorageCase.Default];
            var conversation = GetConversationReference();
            var adapter = new TestAdapter(conversation)
                .Use(new TranscriptLoggerMiddleware(storage));

            var fooId = string.Empty;
            await new TestFlow(adapter, async (context, cancellationToken) =>
            {
                fooId = context.Activity.Id;
                var updateActivity = JsonConvert.DeserializeObject<Activity>(JsonConvert.SerializeObject(context.Activity));
                updateActivity.Text = "updated";
                var response = await context.UpdateActivityAsync(updateActivity);
            })
            .Send("start")
            .StartTestAsync();

            var page = await GetPagedResultWithRetryAsync(
                storage: storage,
                conversation: conversation,
                finishWhen: p => p.Items.Length == 2 && p.Items[1].AsMessageActivity().Text == "updated");
            var activities = page.Items.Cast<Activity>().ToList();

            Assert.Equal(2, activities.Count);
            Assert.Equal(fooId, activities[0].Id);
            Assert.Equal("start", activities[0].Text);
            Assert.StartsWith("g_", activities[1].Id);
            Assert.Equal("updated", activities[1].Text);
        }

        [Fact]
        protected virtual async Task TranscriptLoggerMiddleware_DeleteActivity()
        {
            var storage = Storages[StorageCase.Default];
            var conversation = GetConversationReference();
            var adapter = new TestAdapter(conversation)
                .Use(new TranscriptLoggerMiddleware(storage));
            string activityId = null;

            await new TestFlow(adapter, async (context, cancellationToken) =>
            {
                if (context.Activity.Text == "delete")
                {
                    await context.DeleteActivityAsync(activityId);
                }
                else
                {
                    var activity = context.Activity.CreateReply("started");
                    var response = await context.SendActivityAsync(activity);
                    activityId = response.Id;
                }
            })
            .Send("start")
            .AssertReply("started")
            .Delay(300)
            .Send("delete")
            .StartTestAsync();

            var page = await GetPagedResultWithRetryAsync(
                storage: storage,
                conversation: conversation,
                finishWhen: p => p.Items.Length == 3 && p.Items[1].Type == ActivityTypes.MessageDelete);
            var activities = page.Items.Cast<Activity>().ToList();

            Assert.Equal(3, activities.Count);
            Assert.Equal("start", activities[0].Text);
            Assert.Equal(ActivityTypes.MessageDelete, activities[1].Type);
            Assert.Equal("delete", activities[2].Text);
        }

        protected Activity CloneActivity(Activity activity)
        {
            return JsonConvert.DeserializeObject<Activity>(JsonConvert.SerializeObject(activity));
        }

        protected Activity GetActivity(string id = default, string channelId = default, string conversationId = default)
        {
            return new Activity
            {
                Id = $"activity-{id ?? testName}",
                ChannelId = $"channel-{channelId ?? testName}",
                Text = $"text-{testName}",
                Type = ActivityTypes.Message,
                Conversation = new ConversationAccount(id: $"conversation-{conversationId ?? testName}"),
                Timestamp = DateTime.Now,
                From = new ChannelAccount($"user-{testName}"),
                Recipient = new ChannelAccount($"bot-{testName}"),
            };
        }

        protected ConversationReference GetConversationReference(string channelId = default, string conversationId = default)
        {
            var conversation = $"conversation-{conversationId ?? testName}";
            return new ConversationReference
            {
                ChannelId = $"channel-{channelId ?? testName}",
                ServiceUrl = "https://test.com",
                Conversation = new ConversationAccount(false, conversation, conversation),
                User = new ChannelAccount($"user-{testName}"),
                Bot = new ChannelAccount($"bot-{testName}"),
                Locale = "en-us"
            };
        }

        // NOTE: There are some async oddities within TranscriptLoggerMiddleware that make it difficult to set a short delay when running these tests that ensures
        // the TestFlow completes while also logging transcripts. Some tests will not pass without longer delays, but this method minimizes the delay required.
        protected async Task<PagedResult<IActivity>> GetPagedResultWithRetryAsync(ITranscriptStore storage, ConversationReference conversation, Func<PagedResult<IActivity>, bool> finishWhen, string continuationToken = null, DateTime startDate = default)
        {
            const int maxTimeout = 10000;
            const int delay = 300;
            PagedResult<IActivity> page = null;

            for (var timeout = 0; timeout < maxTimeout; timeout += delay)
            {
                await Task.Delay(delay);
                try
                {
                    page = await storage.GetTranscriptActivitiesAsync(conversation.ChannelId, conversation.Conversation.Id, continuationToken, startDate);
                    if (finishWhen(page))
                    {
                        break;
                    }
                }
                catch (KeyNotFoundException)
                {
                }
                catch (NullReferenceException)
                {
                }
            }

            if (page == null)
            {
                throw new TimeoutException("Storage: Unable to retrieve the 'PagedResult' in time from the Blobs service.");
            }

            return page;
        }
    }
}
