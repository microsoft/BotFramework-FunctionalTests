// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ActivityEx } = require('botbuilder-core');
const { SkillDefinition } = require('./skillDefinition');

class EchoSkill extends SkillDefinition {
  getActions () {
    return new Set(['Message']);
  }

  /**
   * @param {string} actionId
   */
  createBeginActivity (actionId) {
    if (!this.getActions().has(actionId)) {
      throw new Error(`[EchoSkill]: Unable to create begin activity for "${actionId}".`);
    }

    // We only support one activity for Echo so no further checks are needed
    const activity = ActivityEx.createMessageActivity();
    activity.name = actionId;
    activity.text = 'Begin the Echo Skill.';

    return activity;
  }
}

module.exports.EchoSkill = EchoSkill;
