// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using Xunit.Abstractions;

namespace SkillFunctionalTests.Common
{
    public class TestCaseDataObject : IXunitSerializable
    {
        private const string TestIdKey = "TestIdKey";
        private const string TestObjectKey = "TestObjectKey";
        private string _testObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestCaseDataObject"/> class.
        /// </summary>
        public TestCaseDataObject()
        {
            // Note: This empty constructor is needed by the serializer.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestCaseDataObject"/> class.
        /// </summary>
        /// <param name="id">An identifier for the test case.</param>
        /// <param name="testData">An object with the data to be used in the test.</param>
        public TestCaseDataObject(string id, object testData)
        {
            Id = id;
            _testObject = JsonConvert.SerializeObject(testData);
        }

        /// <summary>
        /// Gets a json string with the test data object.
        /// </summary>
        /// <value>The test data object as a json string.</value>
        public string Id { get; private set; }

        /// <summary>
        /// Used by XUnit.net for deserialization.
        /// </summary>
        /// <param name="serializationInfo">A parameter used by XUnit.net.</param>
        public void Deserialize(IXunitSerializationInfo serializationInfo)
        {
            Id = serializationInfo.GetValue<string>(TestIdKey);
            _testObject = serializationInfo.GetValue<string>(TestObjectKey);
        }

        /// <summary>
        /// Used by XUnit.net for serialization.
        /// </summary>
        /// <param name="serializationInfo">A parameter used by XUnit.net.</param>
        public void Serialize(IXunitSerializationInfo serializationInfo)
        {
            serializationInfo.AddValue(TestIdKey, Id);
            serializationInfo.AddValue(TestObjectKey, _testObject);
        }

        /// <summary>
        /// Gets the test data object for the specified .Net type.
        /// </summary>
        /// <typeparam name="T">The type of the object to be returned.</typeparam>
        /// <returns>The test object instance.</returns>
        public T GetObject<T>()
        {
            return JsonConvert.DeserializeObject<T>(_testObject);
        }
    }
}
