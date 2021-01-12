// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using TranscriptTestRunner;

namespace SkillFunctionalTests.Common
{
    public class TestCaseBuilder
    {
        public IEnumerable<object[]> BuildTestCases(List<ClientType> clientTypes, List<string> deliveryModes, List<string> hostBots, List<string> targetSkills, List<string> scripts)
        {
            var testCases = new List<object[]>();
            var count = 1;
            foreach (var clientType in clientTypes)
            {
                foreach (var script in scripts)
                {
                    foreach (var deliveryMode in deliveryModes)
                    {
                        foreach (var hostBot in hostBots)
                        {
                            foreach (var targetSkill in targetSkills)
                            {
                                var testCase = new TestCase
                                {
                                    Id = $"{count:0000}-{script}",
                                    Description = $"Script: {script}, HostBot: {hostBot}, TargetSkill: {targetSkill}, ClientType: {clientType}, DeliverMode: {deliveryMode}",
                                    ClientType = clientType,
                                    DeliveryMode = deliveryMode,
                                    HostBot = hostBot,
                                    TargetSkill = targetSkill,
                                    Script = script
                                };

                                testCases.Add(new object[] { new TestCaseDataObject(testCase.Id, testCase) });
                                count++;
                            }
                        }
                    }
                }
            }

            return testCases;
        }
    }
}
