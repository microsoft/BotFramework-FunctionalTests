// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Documents.Client;
using Xunit;

namespace Microsoft.Bot.Builder.Tests.Integration.Azure.Cosmos
{
    public abstract class CosmosDbBaseFixture : ConfigurationFixture, IAsyncLifetime
    {
        public string AuthKey { get; private set; }

        public string ServiceEndpoint { get; private set; }

        public string DatabaseId { get; private set; }

        public string ContainerId { get; private set; }

        public CosmosClient Client { get; private set; }

        protected bool IsRunning { get; private set; }

        public async Task InitializeAsync()
        {
            var attr = GetType().GetCustomAttribute(typeof(CosmosDbAttribute)) as CosmosDbAttribute;
            DatabaseId = attr?.DatabaseId;
            ContainerId = attr?.ContainerId;

            ServiceEndpoint = Configuration["Azure:Cosmos:ServiceEndpoint"];
            AuthKey = Configuration["Azure:Cosmos:AuthKey"];

            Client = new CosmosClient(
                ServiceEndpoint,
                AuthKey,
                new CosmosClientOptions());

            IsRunning = await IsServiceRunning();

            await Client.CreateDatabaseIfNotExistsAsync(DatabaseId);
        }

        public async Task DisposeAsync()
        {
            if (!IsRunning)
            {
                return;
            }

            await DeleteDatabase(DatabaseId);
        }

        protected async Task<bool> DeleteDatabase(string name)
        {
            try
            {
                using var cancellation = new CancellationTokenSource(Timeout);
                await Client.GetDatabase(name).DeleteAsync(cancellationToken: cancellation.Token);
                return true;
            }
            catch (TaskCanceledException ex)
            {
                const string message = "Cosmos: Error cleaning up resources.";
                throw new TaskCanceledException(message, ex);
            }
        }

        protected async Task<bool> IsServiceRunning()
        {
            using var client = new DocumentClient(new Uri(ServiceEndpoint), AuthKey);
            try
            {
                await client.OpenAsync();
                return true;
            }
            catch (HttpRequestException ex)
            {
                var message = $"Cosmos: Unable to connect to the '{ServiceEndpoint}' endpoint.";
                throw new TaskCanceledException(message, ex);
            }
        }
    }
}
