// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { EchoSkill } = require('./skills/echoSkill');
const { WaterfallSkill } = require('./skills/waterfallSkill');
const { TeamsSkill } = require('./skills/teamsSkill');

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

        if (propName === 'group') {
          acc.groups.add(value);
          acc.entries[id] = this.createSkillDefinition(acc.entries[id]);
        }

        return acc;
      }, { ids: new Set(), appIds: new Set(), groups: new Set(), entries: {} });

    this.skillHostEndpoint = process.env.SkillHostEndpoint;
    if (!this.skillHostEndpoint) {
      throw new Error('[SkillsConfiguration]: Missing configuration parameter. SkillHostEndpoint is required');
    }
  }

  createSkillDefinition (skill) {
    // Note: we hard code this for now, we should dynamically create instances based on the manifests.
    switch (skill.group) {
      case 'Echo':
        return Object.assign(new EchoSkill(), skill);

      case 'Waterfall':
        return Object.assign(new WaterfallSkill(), skill);

      case 'Teams':
        return Object.assign(new TeamsSkill(), skill);

      default:
        throw new Error(`[SkillsConfiguration]: Unable to find definition class for ${skill.id}`);
    }
  }
}

module.exports.SkillsConfiguration = SkillsConfiguration;
