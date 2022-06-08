// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Azure;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Bot.Builder.Tests.Integration.Azure.Storage.Blobs
{
    [Blobs(containerId: "AzureBlobStorageTests")]
    public class AzureBlobStorageFixture : BlobsStorageBaseFixture, IAsyncLifetime
    {
        public IDictionary<StorageCase, IStorage> Storages { get; private set; }

        public new async Task InitializeAsync()
        {
            await base.InitializeAsync();

            var storageAccount = CloudStorageAccount.Parse(ConnectionString);

            Storages = new Dictionary<StorageCase, IStorage>
            {
                { StorageCase.Default, new AzureBlobStorage(storageAccount, ContainerId) },
                { StorageCase.TypeNameHandlingNone, new AzureBlobStorage(storageAccount, ContainerId, new JsonSerializer() { TypeNameHandling = TypeNameHandling.None }) }
            };
        }
    }
}
