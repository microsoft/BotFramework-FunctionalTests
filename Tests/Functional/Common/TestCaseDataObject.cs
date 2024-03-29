﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace Microsoft.Bot.Builder.Tests.Functional.Common
{
    public class TestCaseDataObject<TClass> : IXunitSerializable
    {
        private const string TestObjectKey = "TestObjectKey";
        private string _testObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestCaseDataObject{TClass}"/> class.
        /// </summary>
        public TestCaseDataObject()
        {
            // Note: This empty constructor is needed by the serializer.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestCaseDataObject{TClass}"/> class.
        /// </summary>
        /// <param name="testData">An object with the data to be used in the test.</param>
        public TestCaseDataObject(object testData)
        {
            _testObject = JsonConvert.SerializeObject(testData);
        }

        /// <summary>
        /// Used by XUnit.net for deserialization.
        /// </summary>
        /// <param name="serializationInfo">A parameter used by XUnit.net.</param>
        public void Deserialize(IXunitSerializationInfo serializationInfo)
        {
            _testObject = serializationInfo.GetValue<string>(TestObjectKey);
        }

        /// <summary>
        /// Used by XUnit.net for serialization.
        /// </summary>
        /// <param name="serializationInfo">A parameter used by XUnit.net.</param>
        public void Serialize(IXunitSerializationInfo serializationInfo)
        {
            serializationInfo.AddValue(TestObjectKey, _testObject);
        }

        /// <summary>
        /// Gets the test data object for the specified .Net type.
        /// </summary>
        /// <returns>The test object instance.</returns>
        public TClass GetObject()
        {
            return JsonConvert.DeserializeObject<TClass>(_testObject);
        }

        public override string ToString()
        {
            try
            {
                return GetObject().ToString();
            }
            catch (Exception)
            {
                return base.ToString();
            }
        }
    }
}
