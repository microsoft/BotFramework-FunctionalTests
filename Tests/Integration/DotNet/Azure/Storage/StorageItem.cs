// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Tests.Integration.Azure.Storage
{
    public class StorageItem : IStoreItem
    {
        [JsonProperty(PropertyName = "messageList")]
        public string[] MessageList { get; set; }

        [JsonProperty(PropertyName = "city")]
        public string City { get; set; }

        public string ETag { get; set; }
    }
}
