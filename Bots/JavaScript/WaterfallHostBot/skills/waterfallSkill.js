// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ActivityEx } = require('botbuilder-core');
const { SkillDefinition } = require('./skillDefinition');

class WaterfallSkill extends SkillDefinition {
  getActions () {
    return new Set([
      'Cards',
      'Proactive',
      'Auth',
      'MessageWithAttachment',
      'Sso',
      'FileUpload',
      'Echo',
      'Delete',
      'Update'
    ]);
  }

  /**
   * @param {string} actionId
   */
  createBeginActivity (actionId) {
    if (!this.getActions().has(actionId)) {
      throw new Error(`[WaterfallSkill]: Unable to create begin activity for "${actionId}".`);
    }

    // We don't support special parameters in these skills so a generic event with the right name
    // will do in this case.
    const activity = ActivityEx.createEventActivity();
    activity.name = actionId;

    return activity;
  }
}

module.exports.WaterfallSkill = WaterfallSkill;
