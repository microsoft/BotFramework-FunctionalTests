// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace Microsoft.Bot.Builder.Tests.Integration.Azure.Storage.Blobs
{
    [Trait("TestCategory", "Deprecated")]
    public class AzureBlobStorageTests : StorageBaseTests, IClassFixture<AzureBlobStorageFixture>
    {
        public AzureBlobStorageTests(AzureBlobStorageFixture azureBlobFixture)
        {
            UseStorages(azureBlobFixture.Storages);
        }
    }
}
