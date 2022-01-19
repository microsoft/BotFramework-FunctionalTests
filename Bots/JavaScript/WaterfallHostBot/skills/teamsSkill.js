// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ActivityEx } = require('botbuilder-core');
const { SkillDefinition } = require('./skillDefinition');

class TeamsSkill extends SkillDefinition {
  getActions () {
    return new Set([
      'TeamsTaskModule',
      'TeamsCardAction',
      'TeamsConversation',
      'Cards',
      'Proactive',
      'Attachment',
      'Auth',
      'Sso',
      'Echo',
      'FileUpload',
      'Delete',
      'Update'
    ]);
  }

  /**
   * @param {string} actionId
   */
  createBeginActivity (actionId) {
    if (!this.getActions().has(actionId)) {
      throw new Error(`[TeamsSkill]: Unable to create begin activity for "${actionId}".`);
    }

    // We don't support special parameters in these skills so a generic event with the right name
    // will do in this case.
    const activity = ActivityEx.createEventActivity();
    activity.name = actionId;

    return activity;
  }
}

module.exports.TeamsSkill = TeamsSkill;
