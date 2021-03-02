// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { SkillConversationIdFactoryBase, TurnContext } = require('botbuilder');

/**
 * A SkillConversationIdFactory that uses an in memory dictionary
 * to store and retrieve ConversationReference instances.
 */
class SkillConversationIdFactory extends SkillConversationIdFactoryBase {
    constructor() {
        super();
        this.refs = {};
    }

    /**
     * @param {import('botbuilder').SkillConversationIdFactoryOptions} options
     */
    async createSkillConversationIdWithOptions(options) {
        const skillConversationReference = {
            conversationReference: TurnContext.getConversationReference(options.activity),
            oAuthScope: options.fromBotOAuthScope
        };
        // This key has a 100 character limit by default. Increase with `restify.createServer({ maxParamLength: 1000 });` in index.js.
        const key = `${ skillConversationReference.conversationReference.conversation.id }-${ options.botFrameworkSkill.id }-${ skillConversationReference.conversationReference.channelId }-skillconvo`;
        this.refs[key] = skillConversationReference;
        return key;
    }

    /**
     * @param {string} skillConversationId
     */
    async getSkillConversationReference(skillConversationId) {
        if (!skillConversationId) throw new Error('[SkillConversationIdFactory]: Missing parameter. skillConversationId is required');

        return this.refs[skillConversationId];
    }

    /**
     * @param {string} skillConversationId
     */
    async deleteConversationReference(skillConversationId) {
        this.refs[skillConversationId] = undefined;
    }
}

module.exports.SkillConversationIdFactory = SkillConversationIdFactory;
