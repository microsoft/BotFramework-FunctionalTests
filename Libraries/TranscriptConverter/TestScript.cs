﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Testing.TranscriptConverter
{
    /// <summary>
    /// A Test Script that can be used for functional testing of bots.
    /// </summary>
    public class TestScript
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestScript"/> class.
        /// </summary>
        /// <param name="items">The sequence of test scripts to perform to validate the bots behavior.</param>
        public TestScript(List<TestScriptItem> items = default)
        {
            Items = items ?? new List<TestScriptItem>();
        }

        /// <summary>
        /// Gets the test script items.
        /// </summary>
        /// <value>
        /// The sequence of test scripts to perform to validate the bots behavior.
        /// </value>
        [JsonProperty("items")]
        public List<TestScriptItem> Items { get; } = new List<TestScriptItem>();
    }
}
