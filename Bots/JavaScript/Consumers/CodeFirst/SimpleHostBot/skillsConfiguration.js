// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/**
 * A helper class that loads Skills information from configuration.
 */
class SkillsConfiguration {
  constructor () {
    this.skills = Object.entries(process.env)
      .filter(([key]) => key.startsWith('skill_'))
      .reduce((acc, [key, value]) => {
        const [, id, attr] = key.split('_');

        acc.entries[id] = acc.entries[id] || { id };

        const propName = { appid: 'appId', endpoint: 'skillEndpoint', group: 'group' }[attr.toLowerCase()];
        if (!propName) { throw new Error(`[SkillsConfiguration]: Invalid environment variable declaration ${key}`); }

        acc.entries[id][propName] = value;

        !acc.ids.has(id) && acc.ids.add(id);
        propName === 'appId' && acc.appIds.add(value);
        propName === 'group' && acc.groups.add(value);

        return acc;
      }, { ids: new Set(), appIds: new Set(), groups: new Set(), entries: {} });

    this.skillHostEndpoint = process.env.SkillHostEndpoint;
    if (!this.skillHostEndpoint) {
      throw new Error('[SkillsConfiguration]: Missing configuration parameter. SkillHostEndpoint is required');
    }
  }
}

module.exports.SkillsConfiguration = SkillsConfiguration;
