// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/**
 * Bot Configuration
 */
class DefaultConfig {
  constructor () {
    const {
      MicrosoftAppId,
      MicrosoftAppPassword,
      ConnectionName,
      SsoConnectionName,
      ChannelService,
      AllowedCallers,
      SkillHostEndpoint,
      port,
      PORT
    } = process.env;

    this.ServerUrl = '';
    this.Port = port || PORT || 36420;
    this.MicrosoftAppId = MicrosoftAppId;
    this.MicrosoftAppPassword = MicrosoftAppPassword;
    this.ConnectionName = ConnectionName;
    this.SsoConnectionName = SsoConnectionName;
    this.ChannelService = ChannelService;
    this.SkillHostEndpoint = SkillHostEndpoint;
    // To add a new parent bot, simply edit the .env file and add
    // the parent bot's Microsoft AppId to the list under AllowedCallers, e.g.:
    // AllowedCallers=195bd793-4319-4a84-a800-386770c058b2,38c74e7a-3d01-4295-8e66-43dd358920f8
    this.AllowedCallers = (AllowedCallers || '').split(',').filter((val) => val) || [];
    this.EchoSkillInfo = {
      id: process.env.EchoSkillInfo_id,
      appId: process.env.EchoSkillInfo_appId,
      skillEndpoint: process.env.EchoSkillInfo_skillEndpoint
    };
  }

  /**
   * @param {import('restify').Request} request
   */
  configureServerUrl (request) {
    // Workaround for Restify known issues to construct the server.url.
    // Restify:
    //   [#1029](https://github.com/restify/node-restify/issues/1029)
    //   [#1274](https://github.com/restify/node-restify/issues/1274)
    const protocol = request.headers['x-appservice-proto'] ?? 'http';
    const url = process.env.WEBSITE_HOSTNAME ?? request.headers.host;
    this.ServerUrl = `${protocol}://${url}`;
  }
}

module.exports.DefaultConfig = DefaultConfig;
