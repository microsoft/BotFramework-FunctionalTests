// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { SkillConversationIdFactoryBase, TurnContext } = require('botbuilder');

/**
 * A SkillConversationIdFactory that uses an in memory dictionary to store and retrieve ConversationReference instances.
 */
class SkillConversationIdFactory extends SkillConversationIdFactoryBase {
  constructor () {
    super();
    this.refs = {};
  }

  /**
     * @param {import('botbuilder').SkillConversationIdFactoryOptions} options
     */
  async createSkillConversationIdWithOptions (options) {
    if (!options) {
      throw new Error("Argument 'options' is null.");
    }

    // Create the storage key based on the SkillConversationIdFactoryOptions
    const conversationReference = TurnContext.getConversationReference(options.activity);
    const key = `${conversationReference.conversation.id}-${options.botFrameworkSkill.id}-${conversationReference.channelId}-skillconvo`;

    // Store the SkillConversationReference
    this.refs[key] = {
      conversationReference,
      oAuthScope: options.fromBotOAuthScope
    };

    // Return the storageKey (that will be also used as the conversation ID to call the skill)
    return key;
  }

  /**
     * @param {string} skillConversationId
     */
  async getSkillConversationReference (skillConversationId) {
    if (!skillConversationId) {
      throw new Error("Argument 'skillConversationId' is null or empty.");
    }
    // Get the SkillConversationReference from storage for the given skillConversationId.
    return this.refs[skillConversationId];
  }

  /**
     * @param {string} skillConversationId
     */
  async deleteConversationReference (skillConversationId) {
    // Delete the SkillConversationReference from storage
    this.refs[skillConversationId] = undefined;
  }
}

module.exports.SkillConversationIdFactory = SkillConversationIdFactory;
