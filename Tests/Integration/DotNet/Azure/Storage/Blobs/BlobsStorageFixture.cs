// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Azure.Blobs;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Bot.Builder.Tests.Integration.Azure.Storage.Blobs
{
    [Blobs(containerId: "BlobsStorageTests")]
    public class BlobsStorageFixture : BlobsStorageBaseFixture, IAsyncLifetime
    {
        public IDictionary<StorageCase, IStorage> Storages { get; private set; }

        public new async Task InitializeAsync()
        {
            await base.InitializeAsync();

            Storages = new Dictionary<StorageCase, IStorage>
            {
                { StorageCase.Default, new BlobsStorage(ConnectionString, ContainerId) },
                { StorageCase.TypeNameHandlingNone, new BlobsStorage(ConnectionString, ContainerId, new JsonSerializer() { TypeNameHandling = TypeNameHandling.None }) }
            };
        }
    }
}
