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

        public TeamsSkillBot(ActivityLog log, List<string> activityIds, IHttpClientFactory clientFactory, IConfiguration config)
        {
            _clientFactory = clientFactory;
            _log = log;
            _activityIds = activityIds;
            _config = config;
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
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (turnContext.Activity.Text != null)
            {
                turnContext.Activity.RemoveRecipientMention();
                string actualText = turnContext.Activity.Text;
                if (!string.IsNullOrWhiteSpace(actualText))
                {
                    actualText = actualText.Trim();
                    await HandleBotCommand(turnContext, actualText, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("App sent a message with empty text"), cancellationToken).ConfigureAwait(false);
                if (turnContext.Activity.Value != null)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"but with value {JsonConvert.SerializeObject(turnContext.Activity.Value)}"), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        protected override Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionSubmitActionAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return action.CommandId switch
            {
                "createCard" => IntegrationCommandHandler.CreateCardCommand(action),
                "shareMessage" => IntegrationCommandHandler.ShareMessageCommand(action),
                "createWithPreview" => IntegrationCommandHandler.CreateMessagePreview(action),
                    #pragma warning disable RCS1079 // Throwing of new NotImplementedException.
                _ => throw new NotImplementedException($"Invalid CommandId: {action.CommandId}"),
                    #pragma warning restore RCS1079 // Throwing of new NotImplementedException.
            };
        }

        protected override Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionFetchTaskAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
        {
            var adaptiveCardEditor = CardSampleHelper.CreateAdaptiveCardEditor();

            return Task.FromResult(new MessagingExtensionActionResponse
            {
                Task = new TaskModuleContinueResponse
                {
                    Value = new TaskModuleTaskInfo
                    {
                        Card = new Attachment
                        {
                            Content = adaptiveCardEditor,
                            ContentType = AdaptiveCard.ContentType,
                        },
                        Height = 450,
                        Width = 500,
                        Title = "Task Module Fetch Example",
                    },
                },
            });
        }

        protected override Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionBotMessagePreviewEditAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            // The data has been returned to the bot in the action structure.
            var activityPreview = action.BotActivityPreview[0];
            var attachmentContent = activityPreview.Attachments[0].Content;
            var previewedCard = JsonConvert.DeserializeObject<AdaptiveCard>(attachmentContent.ToString(), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var sampleData = CardSampleHelper.CreateSampleData(previewedCard);

            // This is a preview edit call and so this time we want to re-create the adaptive card editor.
            var adaptiveCardEditor = CardSampleHelper.CreateAdaptiveCardEditor(sampleData);

            return Task.FromResult(new MessagingExtensionActionResponse
            {
                Task = new TaskModuleContinueResponse
                {
                    Value = new TaskModuleTaskInfo
                    {
                        Card = new Attachment
                        {
                            Content = adaptiveCardEditor,
                            ContentType = AdaptiveCard.ContentType,
                        },
                        Height = 450,
                        Width = 500,
                        Title = "Task Module Fetch Example",
                    },
                },
            });
        }

        protected override async Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionBotMessagePreviewSendAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            // The data has been returned to the bot in the action structure.
            var activityPreview = action.BotActivityPreview[0];
            var attachmentContent = activityPreview.Attachments[0].Content;
            var previewedCard = JsonConvert.DeserializeObject<AdaptiveCard>(attachmentContent.ToString(), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            var sampleData = CardSampleHelper.CreateSampleData(previewedCard);

            // This is a send so we are done and we will create the adaptive card editor.
            var adaptiveCard = CardSampleHelper.CreateAdaptiveCard(sampleData);

            var message = MessageFactory.Attachment(new Attachment { ContentType = AdaptiveCard.ContentType, Content = adaptiveCard });

            // THIS WILL WORK IF THE BOT IS INSTALLED. (SendActivityAsync will throw if the bot is not installed.)
            await turnContext.SendActivityAsync(message, cancellationToken).ConfigureAwait(false);

            return null;
        }

        protected override async Task OnTeamsMessagingExtensionCardButtonClickedAsync(ITurnContext<IInvokeActivity> turnContext, JObject obj, CancellationToken cancellationToken)
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            // If the adaptive card was added to the compose window (by either the OnTeamsMessagingExtensionSubmitActionAsync or
            // OnTeamsMessagingExtensionBotMessagePreviewSendAsync handler's return values) the submit values will come in here.
            var reply = MessageFactory.Text("OnTeamsMessagingExtensionCardButtonClickedAsync Value: " + JsonConvert.SerializeObject(turnContext.Activity.Value));
            await turnContext.SendActivityAsync(reply, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task<TaskModuleResponse> OnTeamsTaskModuleFetchAsync(ITurnContext<IInvokeActivity> turnContext, TaskModuleRequest taskModuleRequest, CancellationToken cancellationToken)
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            var reply = MessageFactory.Text("handleTeamsTaskModuleFetchAsync TaskModuleRequest: " + JsonConvert.SerializeObject(taskModuleRequest));
            await turnContext.SendActivityAsync(reply, cancellationToken).ConfigureAwait(false);

            var adaptiveCard = new AdaptiveCard(new AdaptiveSchemaVersion(1, 2));
            adaptiveCard.Body.Add(new AdaptiveTextBlock("This is an Adaptive Card within a Task Module"));
            adaptiveCard.Actions.Add(new AdaptiveSubmitAction { Type = "Action.Submit", Title = "Action.Submit", Data = new JObject { { "submitLocation", "taskModule" } } });

            return new TaskModuleResponse
            {
                Task = new TaskModuleContinueResponse
                {
                    Value = new TaskModuleTaskInfo()
                    {
                        Card = adaptiveCard.ToAttachment(),
                        Height = 200,
                        Width = 400,
                        Title = "Task Module Example",
                    },
                },
            };
        }

        protected override async Task<TaskModuleResponse> OnTeamsTaskModuleSubmitAsync(ITurnContext<IInvokeActivity> turnContext, TaskModuleRequest taskModuleRequest, CancellationToken cancellationToken)
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            await turnContext.SendActivityAsync(MessageFactory.Text($"handleTeamsTaskModuleFetchAsync Value: {JsonConvert.SerializeObject(taskModuleRequest)}"), cancellationToken).ConfigureAwait(false);

            return new TaskModuleResponse
            {
                Task = new TaskModuleMessageResponse()
                {
                    Value = "Thanks!",
                },
            };
        }

        protected override async Task<InvokeResponse> OnTeamsCardActionInvokeAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            var value = turnContext.Activity.Value.ToString().Replace("\r\n", string.Empty).Replace(" ", string.Empty).Trim();
            await turnContext.SendActivityAsync(MessageFactory.Text($"handleTeamsCardActionInvoke value: {value}"), cancellationToken).ConfigureAwait(false);
            return new InvokeResponse() { Status = 200 };
        }

        protected override async Task OnTeamsChannelRenamedAsync(ChannelInfo channelInfo, TeamInfo teamInfo, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (channelInfo == null)
            {
                throw new ArgumentNullException(nameof(channelInfo));
            }

            var heroCard = new HeroCard(text: $"{channelInfo.Name} is the new Channel name");
            await turnContext.SendActivityAsync(MessageFactory.Attachment(heroCard.ToAttachment()), cancellationToken).ConfigureAwait(false);
        }

        protected override async Task OnTeamsChannelCreatedAsync(ChannelInfo channelInfo, TeamInfo teamInfo, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (channelInfo == null)
            {
                throw new ArgumentNullException(nameof(channelInfo));
            }

            var heroCard = new HeroCard(text: $"{channelInfo.Name} is the Channel created");
            await turnContext.SendActivityAsync(MessageFactory.Attachment(heroCard.ToAttachment()), cancellationToken).ConfigureAwait(false);
        }

        protected override async Task OnTeamsChannelDeletedAsync(ChannelInfo channelInfo, TeamInfo teamInfo, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (channelInfo == null)
            {
                throw new ArgumentNullException(nameof(channelInfo));
            }

            var heroCard = new HeroCard(text: $"{channelInfo.Name} is the Channel deleted");
            await turnContext.SendActivityAsync(MessageFactory.Attachment(heroCard.ToAttachment()), cancellationToken).ConfigureAwait(false);
        }

        protected override async Task OnTeamsTeamRenamedAsync(TeamInfo teamInfo, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (teamInfo == null)
            {
                throw new ArgumentNullException(nameof(teamInfo));
            }

            var heroCard = new HeroCard(text: $"{teamInfo.Name} is the new Team name");
            await turnContext.SendActivityAsync(MessageFactory.Attachment(heroCard.ToAttachment()), cancellationToken).ConfigureAwait(false);
        }

        protected override async Task OnTeamsMembersAddedAsync(IList<TeamsChannelAccount> membersAdded, TeamInfo teamInfo, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            var location = teamInfo == null ? turnContext.Activity.Conversation.ConversationType : teamInfo.Name;
            var heroCard = new HeroCard(text: $"{string.Join(' ', membersAdded.Select(member => member.Id))} joined {location}");
            await turnContext.SendActivityAsync(MessageFactory.Attachment(heroCard.ToAttachment()), cancellationToken).ConfigureAwait(false);
        }

        protected override async Task OnTeamsMembersRemovedAsync(IList<TeamsChannelAccount> membersRemoved, TeamInfo teamInfo, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            var location = teamInfo == null ? turnContext.Activity.Conversation.ConversationType : teamInfo.Name;
            var heroCard = new HeroCard(text: $"{string.Join(' ', membersRemoved.Select(member => member.Id))} removed from {location}");
            await turnContext.SendActivityAsync(MessageFactory.Attachment(heroCard.ToAttachment()), cancellationToken).ConfigureAwait(false);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            var heroCard = new HeroCard(text: $"{string.Join(' ', membersAdded.Select(member => member.Id))} joined {turnContext.Activity.Conversation.ConversationType}");
            await turnContext.SendActivityAsync(MessageFactory.Attachment(heroCard.ToAttachment()), cancellationToken).ConfigureAwait(false);
        }

        protected override async Task OnMembersRemovedAsync(IList<ChannelAccount> membersRemoved, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            var heroCard = new HeroCard(text: $"{string.Join(' ', membersRemoved.Select(member => member.Id))} removed from {turnContext.Activity.Conversation.ConversationType}");
            await turnContext.SendActivityAsync(MessageFactory.Attachment(heroCard.ToAttachment()), cancellationToken).ConfigureAwait(false);
        }

        protected override async Task<MessagingExtensionResponse> OnTeamsAppBasedLinkQueryAsync(ITurnContext<IInvokeActivity> turnContext, AppBasedLinkQuery query, CancellationToken cancellationToken)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var heroCard = new ThumbnailCard
            {
                Title = "Thumbnail Card",
                Text = query.Url,
                Images = new List<CardImage> { new CardImage(Constants.BotFrameworkIconUrl) },
            };

            var attachments = new MessagingExtensionAttachment(HeroCard.ContentType, null, heroCard);
            var result = new MessagingExtensionResult(AttachmentLayoutTypes.List, "result", new[] { attachments }, null, "test unfurl");

            return await Task.FromResult(new MessagingExtensionResponse(result)).ConfigureAwait(false);
        }

        protected override async Task OnReactionsAddedAsync(IList<MessageReaction> messageReactions, ITurnContext<IMessageReactionActivity> turnContext, CancellationToken cancellationToken)
        {
            if (messageReactions == null)
            {
                throw new ArgumentNullException(nameof(messageReactions));
            }

            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            foreach (var reaction in messageReactions)
            {
                // The ReplyToId property of the inbound MessageReaction Activity will correspond to a Message Activity that was previously sent from this bot.
                var activity = await _log.Find(turnContext.Activity.ReplyToId).ConfigureAwait(false);
                if (activity == null)
                {
                    // If we had sent the message from the error handler we wouldn't have recorded the Activity Id and so we shouldn't expect to see it in the log.
                    await SendMessageAndLogActivityIdAsync(turnContext, $"Activity {turnContext.Activity.ReplyToId} not found in the log.", cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await SendMessageAndLogActivityIdAsync(turnContext, $"You added '{reaction.Type}' regarding '{activity.Text}'", cancellationToken).ConfigureAwait(false);
                }  
            }
        }

        protected override async Task OnReactionsRemovedAsync(IList<MessageReaction> messageReactions, ITurnContext<IMessageReactionActivity> turnContext, CancellationToken cancellationToken)
        {
            if (messageReactions == null)
            {
                throw new ArgumentNullException(nameof(messageReactions));
            }

            foreach (var reaction in messageReactions)
            {
                if (turnContext == null)
                {
                    throw new ArgumentNullException(nameof(turnContext));
                }

                // The ReplyToId property of the inbound MessageReaction Activity will correspond to a Message Activity that was previously sent from this bot.
                var activity = await _log.Find(turnContext.Activity.ReplyToId).ConfigureAwait(false);
                if (activity == null)
                {
                    // If we had sent the message from the error handler we wouldn't have recorded the Activity Id and so we shouldn't expect to see it in the log.
                    await SendMessageAndLogActivityIdAsync(turnContext, $"Activity {turnContext.Activity.ReplyToId} not found in the log.", cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await SendMessageAndLogActivityIdAsync(turnContext, $"You removed '{reaction.Type}' regarding '{activity.Text}'", cancellationToken).ConfigureAwait(false);
                }  
            }
        }

        protected override async Task<MessagingExtensionResponse> OnTeamsMessagingExtensionConfigurationQuerySettingUrlAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionQuery query, CancellationToken cancellationToken)
        {
            var messageExtensionResponse = new MessagingExtensionResponse
            {
                ComposeExtension = new MessagingExtensionResult
                {
                    Type = "config",
                    SuggestedActions = new MessagingExtensionSuggestedAction
                    {
                        Actions = new List<CardAction>
                        {
                            new CardAction
                            {
                                Type = ActionTypes.OpenUrl,
                                Value = Constants.TeamsSettingsPageUrl,
                            },
                        },
                    },
                },
            };

            return await Task.FromResult(messageExtensionResponse).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override async Task OnTeamsMessagingExtensionConfigurationSettingAsync(ITurnContext<IInvokeActivity> turnContext, JObject settings, CancellationToken cancellationToken)
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            // This event is fired when the settings page is submitted
            var reply = MessageFactory.Text($"handleTeamsMessagingExtensionConfigurationSetting event fired with {settings}");
            await turnContext.SendActivityAsync(reply, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task OnTeamsO365ConnectorCardActionAsync(ITurnContext<IInvokeActivity> turnContext, O365ConnectorCardActionQuery query, CancellationToken cancellationToken)
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            await turnContext.SendActivityAsync(MessageFactory.Text($"O365ConnectorCardActionQuery event value: {JsonConvert.SerializeObject(query)}")).ConfigureAwait(false);
        }

        protected override async Task<MessagingExtensionResponse> OnTeamsMessagingExtensionQueryAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionQuery query, CancellationToken cancellationToken)
        {
            var text = query?.Parameters?[0]?.Value as string ?? string.Empty;

            var packages = await FindPackages(text).ConfigureAwait(false);

            // We take every row of the results and wrap them in cards wrapped in in MessagingExtensionAttachment objects.
            // The Preview is optional, if it includes a Tap, that will trigger the OnTeamsMessagingExtensionSelectItemAsync event back on this bot.
            var attachments = packages.Select(package => 
            {
                var previewCard = new ThumbnailCard { Title = package.Item1, Tap = new CardAction { Type = "invoke", Value = package } };
                if (!string.IsNullOrEmpty(package.Item5))
                {
                    previewCard.Images = new List<CardImage>() { new CardImage(package.Item5, "Icon") };
                }

                var attachment = new MessagingExtensionAttachment
                {
                    ContentType = HeroCard.ContentType,
                    Content = new HeroCard { Title = package.Item1 },
                    Preview = previewCard.ToAttachment()
                };

                return attachment;
            }).ToList();

            // The list of MessagingExtensionAttachments must we wrapped in a MessagingExtensionResult wrapped in a MessagingExtensionResponse.
            return new MessagingExtensionResponse
            {
                ComposeExtension = new MessagingExtensionResult
                {
                    Type = "result",
                    AttachmentLayout = "list",
                    Attachments = attachments
                }
            };
        }

        protected override Task<MessagingExtensionResponse> OnTeamsMessagingExtensionSelectItemAsync(ITurnContext<IInvokeActivity> turnContext, JObject query, CancellationToken cancellationToken)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            // The Preview card's Tap should have a Value property assigned, this will be returned to the bot in this event. 
            var (packageId, version, description, projectUrl, iconUrl) = query.ToObject<(string, string, string, string, string)>();

            // We take every row of the results and wrap them in cards wrapped in in MessagingExtensionAttachment objects.
            // The Preview is optional, if it includes a Tap, that will trigger the OnTeamsMessagingExtensionSelectItemAsync event back on this bot.
            var card = new ThumbnailCard
            {
                Title = $"{packageId}, {version}",
                Subtitle = description,
                Buttons = new List<CardAction>
                    {
                        new CardAction { Type = ActionTypes.OpenUrl, Title = "Nuget Package", Value = $"{Constants.NugetBaseUrl}/packages/{packageId}" },
                        new CardAction { Type = ActionTypes.OpenUrl, Title = "Project", Value = projectUrl },
                    },
            };

            if (!string.IsNullOrEmpty(iconUrl))
            {
                card.Images = new List<CardImage>() { new CardImage(iconUrl, "Icon") };
            }

            var attachment = new MessagingExtensionAttachment
            {
                ContentType = ThumbnailCard.ContentType,
                Content = card,
            };

            return Task.FromResult(new MessagingExtensionResponse
            {
                ComposeExtension = new MessagingExtensionResult
                {
                    Type = "result",
                    AttachmentLayout = "list",
                    Attachments = new List<MessagingExtensionAttachment> { attachment }
                }
            });
        }

        protected override async Task OnTeamsFileConsentAcceptAsync(ITurnContext<IInvokeActivity> turnContext, FileConsentCardResponse fileConsentCardResponse, CancellationToken cancellationToken)
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (fileConsentCardResponse == null)
            {
                throw new ArgumentNullException(nameof(fileConsentCardResponse));
            }

            try
            {
                JToken context = JObject.FromObject(fileConsentCardResponse.Context);

                string filePath = Path.Combine("Files", context["filename"].ToString());
                long fileSize = new FileInfo(filePath).Length;
                using var client = _clientFactory.CreateClient();
                using var fileStream = File.OpenRead(filePath);
                using var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentLength = fileSize;
                fileContent.Headers.ContentRange = new ContentRangeHeaderValue(0, fileSize - 1, fileSize);
                await client.PutAsync(new Uri(fileConsentCardResponse.UploadInfo.UploadUrl), fileContent, cancellationToken).ConfigureAwait(false);

                await FileUploadCompletedAsync(turnContext, fileConsentCardResponse, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await FileUploadFailedAsync(turnContext, e.ToString(), cancellationToken).ConfigureAwait(false);
            }
        }

        protected override async Task OnTeamsFileConsentDeclineAsync(ITurnContext<IInvokeActivity> turnContext, FileConsentCardResponse fileConsentCardResponse, CancellationToken cancellationToken)
        {
            if (fileConsentCardResponse == null)
            {
                throw new ArgumentNullException(nameof(fileConsentCardResponse));
            }

            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            JToken context = JObject.FromObject(fileConsentCardResponse.Context);

            var reply = ((Activity)turnContext.Activity).CreateReply();
            reply.TextFormat = "xml";
            reply.Text = $"Declined. We won't upload file <b>{context["filename"]}</b>.";
            await turnContext.SendActivityAsync(reply, cancellationToken).ConfigureAwait(false);
        }

        private static async Task GetSdkVersion(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(
                Assembly.GetAssembly(typeof(ActivityHandler)).Location);
            string version = fvi.ProductVersion.Split("+")[0];

            var msg = $"{turnContext.Activity.Value} The bot version is {version}.";

            await turnContext.SendActivityAsync(MessageFactory.Text(msg), cancellationToken).ConfigureAwait(false);
        }

        // Generate a set of substrings to illustrate the idea of a set of results coming back from a query. 
        private static async Task<IEnumerable<(string, string, string, string, string)>> FindPackages(string text)
        {
            using var client = new HttpClient();
            var obj = JObject.Parse(await client.GetStringAsync(new Uri($"{Constants.AzureResearchBaseUrl}/query?q=id:{text}&prerelease=true")).ConfigureAwait(false));
            return obj["data"].Select(item => (item["id"].ToString(), item["version"].ToString(), item["description"].ToString(), item["projectUrl"]?.ToString(), item["iconUrl"]?.ToString()));
        } 

        private static async Task FileUploadCompletedAsync(ITurnContext turnContext, FileConsentCardResponse fileConsentCardResponse, CancellationToken cancellationToken)
        {
            var downloadCard = new FileInfoCard
            {
                UniqueId = fileConsentCardResponse.UploadInfo.UniqueId,
                FileType = fileConsentCardResponse.UploadInfo.FileType,
            };

            var asAttachment = new Attachment
            {
                Content = downloadCard,
                ContentType = FileInfoCard.ContentType,
                Name = fileConsentCardResponse.UploadInfo.Name,
                ContentUrl = fileConsentCardResponse.UploadInfo.ContentUrl,
            };

            var reply = turnContext.Activity.CreateReply();
            reply.TextFormat = "xml";
            reply.Text = $"<b>File uploaded.</b> Your file <b>{fileConsentCardResponse.UploadInfo.Name}</b> is ready to download";
            reply.Attachments = new List<Attachment> { asAttachment };

            await turnContext.SendActivityAsync(reply, cancellationToken).ConfigureAwait(false);
        }

        private static async Task FileUploadFailedAsync(ITurnContext turnContext, string error, CancellationToken cancellationToken)
        {
            var reply = turnContext.Activity.CreateReply();
            reply.TextFormat = "xml";
            reply.Text = $"<b>File upload failed.</b> Error: <pre>{error}</pre>";
            await turnContext.SendActivityAsync(reply, cancellationToken).ConfigureAwait(false);
        }

        private async Task ResetBotState(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            await _log.Delete(_activityIds.ToArray<string>()).ConfigureAwait(false);
            _activityIds.Clear();
            await turnContext.SendActivityAsync(MessageFactory.Text("I'm reset. Test away!"), cancellationToken).ConfigureAwait(false);
        }

        private async Task HandleBotCommand(ITurnContext<IMessageActivity> turnContext, string actualText, CancellationToken cancellationToken)
        {
            switch (actualText.ToLowerInvariant())
            {
                case "command:reset":
                    await ResetBotState(turnContext, cancellationToken).ConfigureAwait(false);
                    break;
                case "command:getsdkversions":
                    await GetSdkVersion(turnContext, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    await IntegrationCommandHandler.HandleIntegrationCommand(turnContext, actualText, this, cancellationToken).ConfigureAwait(false);
                    break;
            }
        }
    }
}
