﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace Microsoft.BotFrameworkFunctionalTests.TeamsSkillBot
{
    public class ActivityLog
    {
        private readonly IStorage _storage;

        public ActivityLog(IStorage storage)
        {
            _storage = storage;
        }

        public async Task Append(string activityId, Activity activity)
        {
            if (activityId == null)
            {
                throw new ArgumentNullException(nameof(activityId));
            }

            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            await _storage.WriteAsync(new Dictionary<string, object> { { activityId, activity } }).ConfigureAwait(false);
        }

        public async Task<Activity> Find(string activityId)
        {
            if (activityId == null)
            {
                throw new ArgumentNullException(nameof(activityId));
            }

            var activities = await _storage.ReadAsync(new[] { activityId }).ConfigureAwait(false);
            return activities.Count >= 1 ? (Activity)activities[activityId] : null;
        }

        public async Task Delete(string[] keys)
        {
            await _storage.DeleteAsync(keys).ConfigureAwait(false);
        }
    }
}
