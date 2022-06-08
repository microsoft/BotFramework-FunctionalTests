// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Xunit;

namespace Microsoft.Bot.Builder.Tests.Integration.Azure.Storage.Blobs
{
    public class BlobsStorageBaseFixture : ConfigurationFixture, IAsyncLifetime
    {
        public BlobContainerClient Client { get; private set; }

        public string ConnectionString { get; private set; }

        public string ContainerId { get; private set; }

        protected bool IsRunning { get; private set; }

        public async Task InitializeAsync()
        {
            var attr = GetType().GetCustomAttribute(typeof(BlobsAttribute)) as BlobsAttribute;
            ContainerId = attr?.ContainerId?.ToLower();

            ConnectionString = Configuration["Azure:Storage:ConnectionString"];

            Client = new BlobContainerClient(ConnectionString, ContainerId);

            IsRunning = await IsServiceRunning();

            await Client.CreateIfNotExistsAsync();
        }

        public async Task DisposeAsync()
        {
            if (!IsRunning)
            {
                return;
            }

            await DeleteContainer();
        }

        protected async Task<bool> DeleteContainer()
        {
            try
            {
                using var cancellation = new CancellationTokenSource(Timeout);
                await Client.DeleteIfExistsAsync(cancellationToken: cancellation.Token);
                return true;
            }
            catch (TaskCanceledException ex)
            {
                const string message = "Storage: Error cleaning up 'Blobs' resources.";
                throw new TaskCanceledException(message, ex);
            }
        }

        protected async Task<bool> IsServiceRunning()
        {
            try
            {
                using var cancellation = new CancellationTokenSource(Timeout);
                var client = CloudStorageAccount.Parse(ConnectionString).CreateCloudBlobClient();
                await client.GetServicePropertiesAsync(cancellation.Token);
                return true;
            }
            catch (TaskCanceledException ex)
            {
                const string message = "Storage: Unable to connect to the 'Blobs' endpoint.";
                throw new TaskCanceledException(message, ex);
            }
        }
    }
}
