// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { v4 } = require('uuid');
const { ActivityEx, ActivityTypes, CardFactory, CloudSkillHandler, tokenExchangeOperationName, TurnContext } = require('botbuilder');
const { JwtTokenValidation } = require('botframework-connector');

const WATERFALL_SKILL_BOT = 'WaterfallSkillBot';
const COMPOSER_SKILL_BOT = 'ComposerSkillBot';

/**
 * A CloudSkillHandler specialized to support SSO Token exchanges.
 */
class TokenExchangeSkillHandler extends CloudSkillHandler {
  constructor (adapter, bot, conversationIdFactory, auth, skillsConfig, logger = null) {
    super(adapter, bot, conversationIdFactory, auth);

    if (!adapter) throw new Error('[TokenExchangeSkillHandler]: Missing parameter \'adapter\' is required');
    if (!conversationIdFactory) throw new Error('[TokenExchangeSkillHandler]: Missing parameter \'conversationIdFactory\' is required');
    if (!auth) throw new Error('[TokenExchangeSkillHandler]: Missing parameter \'auth\' is required');
    if (!skillsConfig) throw new Error('[TokenExchangeSkillHandler]: Missing parameter \'skillsConfig\' is required');

    this.adapter = adapter;
    this.auth = auth;
    this.conversationIdFactory = conversationIdFactory;
    this.skillsConfig = skillsConfig;

    this.logger = logger;
    this.botId = process.env.MicrosoftAppId;
  }

  async onSendToConversation (claimsIdentity, conversationId, activity) {
    if (await this.interceptOAuthCards(claimsIdentity, activity)) {
      return { id: v4() };
    }

    return await super.onSendToConversation(claimsIdentity, conversationId, activity);
  }

  async onReplyToActivity (claimsIdentity, conversationId, activityId, activity) {
    if (await this.interceptOAuthCards(claimsIdentity, activity)) {
      return { id: v4() };
    }

    return await super.onReplyToActivity(claimsIdentity, conversationId, activityId, activity);
  }

  getCallingSkill (claimsIdentity) {
    const appId = JwtTokenValidation.getAppIdFromClaims(claimsIdentity.claims);

    if (!appId) {
      return null;
    }

    return Object.values(this.skillsConfig.skills.entries).find(skill => skill.appId === appId);
  }

  async interceptOAuthCards (claimsIdentity, activity) {
    const oauthCardAttachment = activity.attachments ? activity.attachments.find(attachment => attachment.contentType === CardFactory.contentTypes.oauthCard) : null;
    if (!oauthCardAttachment) {
      return false;
    }

    const targetSkill = this.getCallingSkill(claimsIdentity);
    if (!targetSkill) {
      return false;
    }

    const oauthCard = oauthCardAttachment.content;
    if (!oauthCard || !oauthCard.tokenExchangeResource || !oauthCard.tokenExchangeResource.uri) {
      return false;
    }

    const tokenClient = await this.auth.createUserTokenClient(claimsIdentity);
    const context = new TurnContext(this.adapter, activity);
    context.turnState.push('BotIdentity', claimsIdentity);

    // We need to know what connection name to use for the token exchange so we figure that out here
    const connectionName = targetSkill.id.includes(WATERFALL_SKILL_BOT) || targetSkill.id.includes(COMPOSER_SKILL_BOT) ? process.env.SsoConnectionName : process.env.SsoConnectionNameTeams;

    if (!connectionName) {
      throw new Error('[TokenExchangeSkillHandler]: The connection name cannot be null.');
    }

    // AAD token exchange
    try {
      const result = await tokenClient.exchangeToken(activity.recipient.id, connectionName, activity.channelId, { uri: oauthCard.tokenExchangeResource.uri });

      if (result.token && result.token) {
        // If token above is null, then SSO has failed and hence we return false.
        // If not, send an invoke to the skill with the token.
        return await this.sendTokenExchangeInvokeToSkill(activity, oauthCard.tokenExchangeResource.id, result.token, oauthCard.connectionName, targetSkill);
      }
    } catch (exception) {
      // Show oauth card if token exchange fails.
      this.logger.log('Unable to exchange token.', exception);
      return false;
    }

    return false;
  }

  async sendTokenExchangeInvokeToSkill (incomingActivity, id, token, connectionName, targetSkill) {
    const activity = ActivityEx.createReply(incomingActivity);
    activity.type = ActivityTypes.Invoke;
    activity.name = tokenExchangeOperationName;
    activity.value = {
      id: id,
      token: token,
      connectionName: connectionName
    };

    const skillConversationReference = await this.conversationIdFactory.getSkillConversationReference(incomingActivity.conversation.id);
    activity.conversation = incomingActivity.conversation;
    activity.serviceUrl = skillConversationReference.conversationReference.serviceUrl;

    // Route the activity to the skill
    const botFrameworkClient = this.auth.createBotFrameworkClient();
    const response = await botFrameworkClient.postActivity(this.botId, targetSkill.appId, targetSkill.skillEndpoint, this.skillsConfig.skillHostEndpoint, activity.conversation.id, activity);

    // Check response status: true if success, false if failure
    return response.status >= 200 && response.status <= 299;
  }
}

module.exports.TokenExchangeSkillHandler = TokenExchangeSkillHandler;
