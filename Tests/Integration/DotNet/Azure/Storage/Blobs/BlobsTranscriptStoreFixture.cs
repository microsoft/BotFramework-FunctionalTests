// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Azure.Blobs;
using Xunit;

namespace Microsoft.Bot.Builder.Tests.Integration.Azure.Storage.Blobs
{
    [Blobs(containerId: "BlobsTranscriptStoreTests")]
    public class BlobsTranscriptStoreFixture : BlobsStorageBaseFixture, IAsyncLifetime
    {
        public IDictionary<StorageCase, ITranscriptStore> Storages { get; private set; }

        public new async Task InitializeAsync()
        {
            await base.InitializeAsync();

            Storages = new Dictionary<StorageCase, ITranscriptStore>
            {
                { StorageCase.Default, new BlobsTranscriptStore(ConnectionString, ContainerId) },
            };
        }
    }
}
