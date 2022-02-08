// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace IntegrationTests.Azure.Cosmos
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class CosmosDbAttribute : Attribute
    {
        public CosmosDbAttribute(string databaseId = "CosmosDatabase", string containerId = "CosmosContainer")
        {
            DatabaseId = databaseId;
            ContainerId = containerId;
        }

        public string DatabaseId { get; private set; }

        public string ContainerId { get; private set; }
    }
}
