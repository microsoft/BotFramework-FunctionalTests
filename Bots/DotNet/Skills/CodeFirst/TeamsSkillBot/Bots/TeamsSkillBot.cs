// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using DotnetIntegrationBot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotnetIntegrationBot
{
    public class TeamsSkillBot : TeamsActivityHandler
    {
        private List<string> _activityIds;
        private readonly ActivityLog _log;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _connectionName;

        public TeamsSkillBot(ActivityLog log, List<string> activityIds, IHttpClientFactory clientFactory, IConfiguration config)
        {
            _clientFactory = clientFactory;
            _log = log;
            _activityIds = activityIds;
            _config = config;
            _connectionName = _config["ConnectionName"];
        }

        public List<string> GetActivityIds()
        {
            return _activityIds;
        }

        public IConfiguration GetConfig()
        {
            return _config;
        }

        public IHttpClientFactory GetHttpClientFactory()
        {
            return _clientFactory;
        }

        internal async Task SendMessageAndLogActivityIdAsync(ITurnContext turnContext, string text, CancellationToken cancellationToken)
        {
            // We need to record the Activity Id from the Activity just sent in order to understand what the reaction is a reaction too. 
            var replyActivity = MessageFactory.Text(text);
            var resourceResponse = await turnContext.SendActivityAsync(replyActivity, cancellationToken).ConfigureAwait(false);
            await _log.Append(resourceResponse.Id, replyActivity).ConfigureAwait(false);
            _activityIds.Add(resourceResponse.Id);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity.ChannelId != Channels.Msteams)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("This bot is only designed to work in MS Teams."), cancellationToken).ConfigureAwait(false);
            }
            else
            {
                turnContext.Activity.RemoveRecipientMention();
                var text = turnContext.Activity.Text.ToLowerInvariant().Trim();
                switch (text)
                {
                    case "proactivent":
                        await HandleProactiveNonThreadedAsync(turnContext, cancellationToken).ConfigureAwait(false);
                        break;
                    case "proactive":
                        await HandleProactiveThreadedAsync(turnContext, cancellationToken).ConfigureAwait(false);
                        break;
                    case "team details":
                        await ShowDetailsAsync(turnContext, cancellationToken).ConfigureAwait(false);
                        break;
                    case "show channels":
                        await ShowChannelsAsync(turnContext, cancellationToken).ConfigureAwait(false);
                        break;
                    case "team members":
                        await ShowTeamMembers(turnContext, cancellationToken).ConfigureAwait(false);
                        break;
                    case "members":
                        await ShowMembersAsync(turnContext, cancellationToken).ConfigureAwait(false);
                        break;
                    case "member":
                        await ShowMemberAsync(turnContext, cancellationToken).ConfigureAwait(false);
                        break;
                    case "paged team members":
                        await GetPagedTeamMembers(turnContext, cancellationToken).ConfigureAwait(false);
                        break;
                    case "paged members":
                        await GetPagedMembers(turnContext, cancellationToken).ConfigureAwait(false);
                        break;
                    case "team member":
                        await GetTeamMember(turnContext, cancellationToken).ConfigureAwait(false);
                        break;
                    case "send message":
                        await SendMessageToTeamsChannel(turnContext, cancellationToken).ConfigureAwait(false);
                        break;
                    case "auth":
                        await HandleAuth(turnContext, cancellationToken).ConfigureAwait(false);
                        break;
                    case "end":
                    case "stop":
                        await EndConversation(turnContext, cancellationToken).ConfigureAwait(false);
                        break;
                    default:
                        await turnContext.SendActivityAsync(MessageFactory.Text($"You said: {turnContext.Activity.Text}"), cancellationToken).ConfigureAwait(false);
                        break;
                }
            }
        }

        protected override Task OnEndOfConversationActivityAsync(ITurnContext<IEndOfConversationActivity> turnContext, CancellationToken cancellationToken)
        {
            // This will be called if the root bot is ending the conversation.  Sending additional messages should be
            // avoided as the conversation may have been deleted.
            // Perform cleanup of resources if needed.
            return Task.CompletedTask;
        }

        protected override async Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionFetchTaskAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
        {
            var tokenResponse = await (turnContext.Adapter as IUserTokenProvider).GetUserTokenAsync(turnContext, _connectionName, string.Empty, cancellationToken: cancellationToken);
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.Token))
            {
                // There is no token, so the user has not signed in yet.

                // Retrieve the OAuth Sign in Link to use in the MessagingExtensionResult Suggested Actions
                var signInLink = await (turnContext.Adapter as IUserTokenProvider).GetOauthSignInLinkAsync(turnContext, _connectionName, cancellationToken);

                return new MessagingExtensionActionResponse
                {
                    ComposeExtension = new MessagingExtensionResult
                    {
                        Type = "auth",
                        SuggestedActions = new MessagingExtensionSuggestedAction
                        {
                            Actions = new List<CardAction>
                            {
                                new CardAction
                                {
                                    Type = ActionTypes.OpenUrl,
                                    Value = signInLink,
                                    Title = "Bot Service OAuth",
                                },
                            },
                        },
                    },
                };
            }

            // User is already signed in.
            return new MessagingExtensionActionResponse
            {
                Task = new TaskModuleContinueResponse(CreateSignedInTaskModuleTaskInfo()),
            };
        }

        protected override async Task<TaskModuleResponse> OnTeamsTaskModuleFetchAsync(ITurnContext<IInvokeActivity> turnContext, TaskModuleRequest taskModuleRequest, CancellationToken cancellationToken)
        {
            // When a user has successfully signed in, the bot receives the Magic Code from Azure Bot Service in
            // the Activity.Value. Use the magic code to obtain the user token.
            var data = turnContext.Activity.Value as JObject;
            if (data != null && data["state"] != null)
            {
                var tokenResponse = await (turnContext.Adapter as IUserTokenProvider).GetUserTokenAsync(turnContext, _connectionName, data["state"].ToString(), cancellationToken: cancellationToken);
                return new TaskModuleResponse() { Task = new TaskModuleContinueResponse(CreateSignedInTaskModuleTaskInfo(tokenResponse.Token)) };
            }
            else
            {
                var reply2 = MessageFactory.Text("OnTeamsTaskModuleFetchAsync called without 'state' in Activity.Value");
                await turnContext.SendActivityAsync(reply2, cancellationToken);
                return null;
            }
        }

        protected override async Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionSubmitActionAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
        {
            var asJObject = action.Data as JObject;
            if (asJObject != null && asJObject.ContainsKey("key") && asJObject["key"].ToString() == "signout")
            {
                // User clicked the Sign Out button from a Task Module
                await (turnContext.Adapter as IUserTokenProvider).SignOutUserAsync(turnContext, _connectionName, turnContext.Activity.From.Id, cancellationToken);
                await turnContext.SendActivityAsync(MessageFactory.Text($"Signed Out: {turnContext.Activity.From.Name}"), cancellationToken);
            }

            return null;
        }

        private TaskModuleTaskInfo CreateSignedInTaskModuleTaskInfo(string token = "")
        {
            var taskModuleTaskInfo = new TaskModuleTaskInfo
            {
                Card = new Attachment
                {
                    Content = new AdaptiveCard
                    {
                        Body = new List<AdaptiveElement>()
                            {
                                new AdaptiveTextBlock("You are signed in!"),
                                new AdaptiveTextBlock("Send 'Log out' or 'Sign out' to start over."),
                                new AdaptiveTextBlock("(Or click the Sign Out button below.)"),
                            },
                        Actions = new List<AdaptiveAction>()
                            {
                                new AdaptiveSubmitAction() { Title = "Close", Data = new JObject { { "key", "close" } } },
                                new AdaptiveSubmitAction() { Title = "Sign Out", Data = new JObject { { "key", "signout" } } },
                            },
                    },
                    ContentType = AdaptiveCard.ContentType,
                },
                Height = 160,
                Width = 350,
                Title = "Messaging Extension Auth Example",
            };

            if (!string.IsNullOrEmpty(token))
            {
                var card = taskModuleTaskInfo.Card.Content as AdaptiveCard;

                // Embed a child Adaptive Card behind a AdaptiveShowCardAction to display the User's token
                card.Actions.Add(new AdaptiveShowCardAction()
                {
                    Title = "Show Token",
                    Card = new AdaptiveCard
                    {
                        Body = new List<AdaptiveElement>()
                        {
                            new AdaptiveTextBlock($"Your token is {token}") { Wrap = true },
                        },
                    },
                });

                taskModuleTaskInfo.Height = 300;
                taskModuleTaskInfo.Width = 500;
            }

            return taskModuleTaskInfo;
        }

        private async Task HandleAuth(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(turnContext.Activity.Text))
            {
                // Hack around weird behavior of RemoveRecipientMention (it alters the activity.Text)
                string originalText = turnContext.Activity.Text;
                turnContext.Activity.RemoveRecipientMention();
                string text = turnContext.Activity.Text.Replace(" ", string.Empty).ToUpperInvariant();
                turnContext.Activity.Text = originalText;

                if (text.Equals("LOGOUT", StringComparison.Ordinal) || text.Equals("SIGNOUT", StringComparison.Ordinal))
                {
                    await (turnContext.Adapter as IUserTokenProvider).SignOutUserAsync(turnContext, _connectionName, turnContext.Activity.From.Id, cancellationToken).ConfigureAwait(false);

                    await turnContext.SendActivityAsync(MessageFactory.Text($"Signed Out: {turnContext.Activity.From.Name}"), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"echo: {turnContext.Activity.Text}"), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task EndConversation(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var messageText = $"ending conversation from the skill...";
            await turnContext.SendActivityAsync(MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput), cancellationToken).ConfigureAwait(false);
            var endOfConversation = Activity.CreateEndOfConversationActivity();
            endOfConversation.Code = EndOfConversationCodes.CompletedSuccessfully;
            await turnContext.SendActivityAsync(endOfConversation, cancellationToken).ConfigureAwait(false);
        }

        private async Task HandleProactiveNonThreadedAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var teamsChannelId = turnContext.Activity.Conversation.Id;
            var serviceUrl = turnContext.Activity.ServiceUrl;
            var credentials = new MicrosoftAppCredentials(_config["MicrosoftAppId"], _config["MicrosoftAppPassword"]);
            ConversationReference conversationReference = null;
            var proactiveMessage = MessageFactory.Text($"This is a proactive message");
            proactiveMessage.Label = turnContext.Activity.Id;
            var conversationParameters = new ConversationParameters
            {
                IsGroup = false,
                Bot = turnContext.Activity.Recipient,
                Members = new ChannelAccount[] { turnContext.Activity.From },
                TenantId = turnContext.Activity.Conversation.TenantId,
            };

            await ((BotFrameworkAdapter)turnContext.Adapter).CreateConversationAsync(
                teamsChannelId,
                serviceUrl,
                credentials,
                conversationParameters,
                async (turnContext1, cancellationToken1) =>
                {
                    conversationReference = turnContext1.Activity.GetConversationReference();
                    await ((BotFrameworkAdapter)turnContext.Adapter).ContinueConversationAsync(
                        _config["MicrosoftAppId"],
                        conversationReference,
                        async (turnContext2, cancellationToken2) =>
                        {
                            await turnContext2.SendActivityAsync(proactiveMessage, cancellationToken2).ConfigureAwait(false);
                        },
                        cancellationToken1).ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false);
        }

        private async Task HandleProactiveThreadedAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (await TeamScopeCheck(turnContext, cancellationToken).ConfigureAwait(false))
            {
                var activity = MessageFactory.Text($"I will send two messages to this thread");
                await turnContext.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);

                for (var i = 0; i < 2; i++)
                {
                    var replyToActivity = MessageFactory.Text($"This is message {i + 1}/2 that will be sent.");
                    replyToActivity.ApplyConversationReference(activity.GetConversationReference());
                    await turnContext.SendActivityAsync(replyToActivity, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task ShowTeamMembers(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (await TeamScopeCheck(turnContext, cancellationToken).ConfigureAwait(false))
            {
                var members = await TeamsInfo.GetTeamMembersAsync(turnContext, turnContext.Activity.TeamsGetTeamInfo().Id, cancellationToken).ConfigureAwait(false);
                var text = string.Empty;
                foreach (var member in members)
                {
                    text += $" {member.Name}.";
                }

                text += " are the members of this team.";
                await turnContext.SendActivityAsync(MessageFactory.Text(text), cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task GetPagedTeamMembers(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (await TeamScopeCheck(turnContext, cancellationToken).ConfigureAwait(false)) 
            { 
                var members = await TeamsInfo.GetPagedMembersAsync(turnContext, 1, null, cancellationToken).ConfigureAwait(false);
                var text = $"There are {members.Members.Count} people in the team.";

                foreach (var member in members.Members)
                {
                    text += $" {member.Name}";
                }

                await turnContext.SendActivityAsync(MessageFactory.Text(text), cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task GetPagedMembers(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var members = await TeamsInfo.GetPagedMembersAsync(turnContext, 1, null, cancellationToken).ConfigureAwait(false);
            var text = $"There are {members.Members.Count} people in the group.";

            foreach (var member in members.Members)
            {
                text += $" {member.Name}.";
            }

            await turnContext.SendActivityAsync(MessageFactory.Text(text), cancellationToken).ConfigureAwait(false);
        }

        private async Task GetTeamMember(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (await TeamScopeCheck(turnContext, cancellationToken).ConfigureAwait(false))
            {
                var member = await TeamsInfo.GetTeamMemberAsync(turnContext, turnContext.Activity.From.Id, turnContext.Activity.TeamsGetTeamInfo().Id, cancellationToken).ConfigureAwait(false);
                await turnContext.SendActivityAsync(MessageFactory.Text($"Your name is {member.Name}. Your ID is {member.Id}."), cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task SendMessageToTeamsChannel(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (await TeamScopeCheck(turnContext, cancellationToken).ConfigureAwait(false))
            {
                var activity = MessageFactory.Text("This is the start of a new thread in a channel");
                var channelId = turnContext.Activity.TeamsGetTeamInfo().Id;
                var creds = new MicrosoftAppCredentials(_config["MicrosoftAppId"], _config["MicrosoftAppPassword"]);
                await TeamsInfo.SendMessageToTeamsChannelAsync(turnContext, activity, channelId, creds, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ShowMemberAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var account = await TeamsInfo.GetMemberAsync(turnContext, turnContext.Activity.From.Id, cancellationToken).ConfigureAwait(false);
            await turnContext.SendActivityAsync(MessageFactory.Text($"Your name is {account.Name}. Your email address is {account.Email}")).ConfigureAwait(false);
        }

        private async Task ShowMembersAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            await ShowMembersAsync(turnContext, await TeamsInfo.GetMembersAsync(turnContext, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        }

        private async Task ShowMembersAsync(ITurnContext<IMessageActivity> turnContext, IEnumerable<TeamsChannelAccount> teamsChannelAccounts, CancellationToken cancellationToken)
        {
            var replyActivity = MessageFactory.Text($"Total of {teamsChannelAccounts.Count()} members are currently in team");
            await turnContext.SendActivityAsync(replyActivity).ConfigureAwait(false);

            var messages = teamsChannelAccounts
                .Select(teamsChannelAccount => $"{teamsChannelAccount.AadObjectId} --> {teamsChannelAccount.Name} -->  {teamsChannelAccount.UserPrincipalName}");

            await SendInBatchesAsync(turnContext, messages, cancellationToken).ConfigureAwait(false);
        }

        private async Task ShowChannelsAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (await TeamScopeCheck(turnContext, cancellationToken).ConfigureAwait(false))
            {
                var teamId = turnContext.Activity.TeamsGetTeamInfo().Id;
                var channels = await TeamsInfo.GetTeamChannelsAsync(turnContext, "19:a1b98da9f3b64b33906be20eb2b1765d@thread.tacv2", cancellationToken).ConfigureAwait(false);
                var replyActivity = MessageFactory.Text($"Total of {channels.Count} channels are currently in team");
                await turnContext.SendActivityAsync(replyActivity).ConfigureAwait(false);
                var messages = channels.Select(channel => $"{channel.Id} --> {channel.Name}");
                await SendInBatchesAsync(turnContext, messages, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ShowDetailsAsync(ITurnContext<IMessageActivity> turnContext, TeamsSkillBot bot, CancellationToken cancellationToken)
        {
            var teamInfo = turnContext.Activity.TeamsGetTeamInfo();

            if (await TeamScopeCheck(turnContext, cancellationToken).ConfigureAwait(false))
            {
                var teamId = teamInfo.Id;
                var teamDetails = await TeamsInfo.GetTeamDetailsAsync(turnContext, teamId, cancellationToken).ConfigureAwait(false);
                await bot.SendMessageAndLogActivityIdAsync(turnContext, $"The team name is {teamDetails.Name}. The team ID is {teamDetails.Id}. The ADD GroupID is {teamDetails.AadGroupId}.", cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task SendInBatchesAsync(ITurnContext<IMessageActivity> turnContext, IEnumerable<string> messages, CancellationToken cancellationToken)
        {
            var batch = new List<string>();
            foreach (var msg in messages)
            {
                batch.Add(msg);

                if (batch.Count == 10)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(string.Join("<br>", batch)), cancellationToken).ConfigureAwait(false);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text(string.Join("<br>", batch)), cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ShowChannelsAsync(ITurnContext<IMessageActivity> turnContext, TeamsSkillBot bot, CancellationToken cancellationToken)
        {
            if (await TeamScopeCheck(turnContext, cancellationToken).ConfigureAwait(false))
            {
                var teamId = turnContext.Activity.TeamsGetTeamInfo().Id;

                var channels = await TeamsInfo.GetTeamChannelsAsync(turnContext, teamId, cancellationToken).ConfigureAwait(false);

                var replyActivity = MessageFactory.Text($"Total of {channels.Count} channels are currently in team");

                await turnContext.SendActivityAsync(replyActivity).ConfigureAwait(false);

                var messages = channels.Select(channel => $"{channel.Id} --> {channel.Name}");

                await SendInBatchesAsync(turnContext, messages, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ShowDetailsAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var teamInfo = turnContext.Activity.TeamsGetTeamInfo();

            if (teamInfo == null)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("This only works in the team scope"), cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var teamId = teamInfo.Id;
                var teamDetails = await TeamsInfo.GetTeamDetailsAsync(turnContext, teamId, cancellationToken).ConfigureAwait(false);
                await turnContext.SendActivityAsync(MessageFactory.Text($"The team name is {teamDetails.Name}. The team ID is {teamDetails.Id}. The ADD GroupID is {teamDetails.AadGroupId}."), cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<bool> TeamScopeCheck(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity.TeamsGetTeamInfo() == null)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("This method only works in the teams scope. Install the bot in a team."), cancellationToken).ConfigureAwait(false);
                return false;
            }

            return true;
        }
    }
}
