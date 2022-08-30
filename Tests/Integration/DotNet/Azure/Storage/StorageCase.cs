// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Builder.Tests.Integration.Azure.Storage
{
    public enum StorageCase
    {
        /// <summary>
        /// Storage instance with default configuration.
        /// </summary>
        Default,

        /// <summary>
        /// Storage instance with JsonSerializer.TypeNameHandling equals to None configuration.
        /// </summary>
        TypeNameHandlingNone,
    }
}
