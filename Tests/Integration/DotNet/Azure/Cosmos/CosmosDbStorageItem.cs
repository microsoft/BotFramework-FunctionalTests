// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Newtonsoft.Json;

namespace IntegrationTests.Azure.Cosmos
{
    public class CosmosDbStorageItem : IStoreItem
    {
        [JsonProperty(PropertyName = "messageList")]
        public string[] MessageList { get; set; }

        [JsonProperty(PropertyName = "city")]
        public string City { get; set; }

        public string ETag { get; set; }
    }
}
