// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { EchoSkill } = require('./skills/echoSkill');
const { WaterfallSkill } = require('./skills/waterfallSkill');
const { TeamsSkill } = require('./skills/teamsSkill');
const { SkillDefinition } = require('./skills/skillDefinition');

/**
 * A helper class that loads Skills information from configuration.
 */
class SkillsConfiguration {
    constructor() {
        this.skillsData = {};

        var skillVariables = Object.keys(process.env).filter(prop => prop.startsWith('skill_'));

        for (const val of skillVariables) {
            const names = val.split('_');
            const id = names[1];
            const attr = names[2];
            let propName;
            if (!(id in this.skillsData)) {
                this.skillsData[id] = { definition: this.createSkillDefinition({ id }) };
            }
            switch (attr.toLowerCase()) {
            case 'appid':
                propName = 'appId';
                break;
            case 'endpoint':
                propName = 'skillEndpoint';
                break;
            default:
                throw new Error(`[SkillsConfiguration]: Invalid environment variable declaration ${ val }`);
            }

            this.skillsData[id][propName] = process.env[val];
        }

        this.skillHostEndpointValue = process.env.SkillHostEndpoint;
        if (!this.skillHostEndpointValue) {
            throw new Error('[SkillsConfiguration]: Missing configuration parameter. SkillHostEndpoint is required');
        }
    }

    get skills() {
        return this.skillsData;
    }

    get skillHostEndpoint() {
        return this.skillHostEndpointValue;
    }

    createSkillDefinition(skill) {
        // Note: we hard code this for now, we should dynamically create instances based on the manifests.
        // For now, this code creates a strong typed version of the SkillDefinition and copies the info from
        // settings into it.
        let skillDefinition = new SkillDefinition();

        switch (true) {
        case skill.id.startsWith('EchoSkillBot'):
            skillDefinition = Object.assign(new EchoSkill(), skill);
            break;
        case skill.id.startsWith('WaterfallSkillBot'):
            skillDefinition = Object.assign(new WaterfallSkill(), skill);
            break;
        case skill.id.startsWith('TeamsSkillBot'):
            skillDefinition = Object.assign(new TeamsSkill(), skill);
            break;
        default:
            throw new Error(`[SkillsConfiguration]: Unable to find definition class for ${ skill.id }`);
        }

        return skillDefinition;
    }
}

module.exports.SkillsConfiguration = SkillsConfiguration;
