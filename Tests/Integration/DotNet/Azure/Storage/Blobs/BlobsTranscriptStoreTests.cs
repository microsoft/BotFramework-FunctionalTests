// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Bot.Builder.Tests.Integration.Azure.Storage.Blobs
{
    public class BlobsTranscriptStoreTests : BlobsTranscriptStoreBaseTests, IClassFixture<BlobsTranscriptStoreFixture>
    {
        public BlobsTranscriptStoreTests(ITestOutputHelper outputHandler, BlobsTranscriptStoreFixture blobFixture)
            : base(outputHandler)
        {
            UseStorages(blobFixture.Storages);
        }
    }
}
