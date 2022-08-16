// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace Microsoft.Bot.Builder.Tests.Integration.Azure.Storage.Blobs
{
    public class BlobsStorageTests : StorageBaseTests, IClassFixture<BlobsStorageFixture>
    {
        public BlobsStorageTests(BlobsStorageFixture blobFixture)
        {
            UseStorages(blobFixture.Storages);
        }
    }
}
