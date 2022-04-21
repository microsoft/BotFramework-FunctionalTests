// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FunctionalTestsBots.SimpleHostBot.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.FunctionalTestsBots.SimpleHostBot.Bots
{
    public class HostBot : ActivityHandler
    {
        public const string DeliveryModePropertyName = "deliveryModeProperty";
        public const string ActiveSkillPropertyName = "activeSkillProperty";

        private readonly IStatePropertyAccessor<string> _deliveryModeProperty;
        private readonly IStatePropertyAccessor<BotFrameworkSkill> _activeSkillProperty;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateProperty;
        private readonly string _botId;
        private readonly BotFrameworkAuthentication _auth;
        private readonly ConversationState _conversationState;
        private readonly SkillsConfiguration _skillsConfig;
        private readonly SkillConversationIdFactoryBase _conversationIdFactory;
        private readonly Dialog _dialog;

        /// <summary>
        /// Initializes a new instance of the <see cref="HostBot"/> class.
        /// </summary>
        /// <param name="auth">The cloud environment for the bot.</param>
        /// <param name="conversationState">A state management object for the conversation.</param>
        /// <param name="skillsConfig">The skills configuration.</param>
        /// <param name="configuration">The configuration properties.</param>
        /// <param name="conversationIdFactory">The conversation id factory.</param>
        /// <param name="dialog">The dialog to use.</param>
        public HostBot(BotFrameworkAuthentication auth, ConversationState conversationState, SkillsConfiguration skillsConfig, SkillConversationIdFactoryBase conversationIdFactory, IConfiguration configuration, SetupDialog dialog)
        {
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _skillsConfig = skillsConfig ?? throw new ArgumentNullException(nameof(skillsConfig));
            _conversationIdFactory = conversationIdFactory ?? throw new ArgumentNullException(nameof(conversationIdFactory));
            _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
            _dialogStateProperty = _conversationState.CreateProperty<DialogState>("DialogState");
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _botId = configuration.GetSection(MicrosoftAppCredentials.MicrosoftAppIdKey)?.Value;

            // Create state properties to track the delivery mode and active skill.
            _deliveryModeProperty = conversationState.CreateProperty<string>(DeliveryModePropertyName);
            _activeSkillProperty = conversationState.CreateProperty<BotFrameworkSkill>(ActiveSkillPropertyName);
        }

        /// <inheritdoc/>
        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            // Forward all activities except EndOfConversation to the active skill.
            if (turnContext.Activity.Type != ActivityTypes.EndOfConversation)
            {
                // Try to get the active skill
                var activeSkill = await _activeSkillProperty.GetAsync(turnContext, () => null, cancellationToken);

                if (activeSkill != null)
                {
                    var deliveryMode = await _deliveryModeProperty.GetAsync(turnContext, () => null, cancellationToken);

                    // Send the activity to the skill
                    await SendToSkillAsync(turnContext, deliveryMode, activeSkill, cancellationToken);
                    return;
                }
            }

            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occurred during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        /// <summary>
        /// Processes a message activity.
        /// </summary>
        /// <param name="turnContext">Context for the current turn of conversation.</param>
        /// <param name="cancellationToken">CancellationToken propagates notifications that operations should be cancelled.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (_skillsConfig.Skills.ContainsKey(turnContext.Activity.Text))
            {
                var deliveryMode = await _deliveryModeProperty.GetAsync(turnContext, () => null, cancellationToken);
                var selectedSkill = _skillsConfig.Skills[turnContext.Activity.Text];
                var v3Bots = new List<string> { "EchoSkillBotDotNetV3", "EchoSkillBotJSV3" };

                if (selectedSkill != null && deliveryMode == DeliveryModes.ExpectReplies && v3Bots.Contains(selectedSkill.Id))
                {
                    var message = MessageFactory.Text("V3 Bots do not support 'expectReplies' delivery mode.");
                    await turnContext.SendActivityAsync(message, cancellationToken);

                    // Forget delivery mode and skill invocation.
                    await _deliveryModeProperty.DeleteAsync(turnContext, cancellationToken);
                    await _activeSkillProperty.DeleteAsync(turnContext, cancellationToken);

                    // Restart setup dialog
                    await _conversationState.DeleteAsync(turnContext, cancellationToken);
                }
            }

            await _dialog.RunAsync(turnContext, _dialogStateProperty, cancellationToken);
        }

        /// <summary>
        /// Processes an end of conversation activity.
        /// </summary>
        /// <param name="turnContext">Context for the current turn of conversation.</param>
        /// <param name="cancellationToken">CancellationToken propagates notifications that operations should be cancelled.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        protected override async Task OnEndOfConversationActivityAsync(ITurnContext<IEndOfConversationActivity> turnContext, CancellationToken cancellationToken)
        {
            await EndConversation((Activity)turnContext.Activity, turnContext, cancellationToken);
        }

        /// <summary>
        /// Processes a member added event.
        /// </summary>
        /// <param name="membersAdded">The list of members added to the conversation.</param>
        /// <param name="turnContext">Context for the current turn of conversation.</param>
        /// <param name="cancellationToken">CancellationToken propagates notifications that operations should be cancelled.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text("Hello and welcome!"), cancellationToken);
                    await _dialog.RunAsync(turnContext, _dialogStateProperty, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Clears storage variables and sends the end of conversation activities.
        /// </summary>
        /// <param name="activity">End of conversation activity.</param>
        /// <param name="turnContext">Context for the current turn of conversation.</param>
        /// <param name="cancellationToken">CancellationToken propagates notifications that operations should be cancelled.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        private async Task EndConversation(Activity activity, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // Forget delivery mode and skill invocation.
            await _deliveryModeProperty.DeleteAsync(turnContext, cancellationToken);
            await _activeSkillProperty.DeleteAsync(turnContext, cancellationToken);

            // Show status message, text and value returned by the skill
            var eocActivityMessage = $"Received {ActivityTypes.EndOfConversation}.\n\nCode: {activity.Code}.";
            if (!string.IsNullOrWhiteSpace(activity.Text))
            {
                eocActivityMessage += $"\n\nText: {activity.Text}";
            }

            if (activity.Value != null)
            {
                eocActivityMessage += $"\n\nValue: {JsonConvert.SerializeObject(activity.Value)}";
            }

            await turnContext.SendActivityAsync(MessageFactory.Text(eocActivityMessage), cancellationToken);

            // We are back at the host.
            await turnContext.SendActivityAsync(MessageFactory.Text("Back in the host bot."), cancellationToken);

            // Restart setup dialog.
            await _dialog.RunAsync(turnContext, _dialogStateProperty, cancellationToken);

            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        /// <summary>
        /// Sends an activity to the skill bot.
        /// </summary>
        /// <param name="turnContext">Context for the current turn of conversation.</param>
        /// <param name="deliveryMode">The delivery mode to use when communicating to the skill.</param>
        /// <param name="targetSkill">The skill that will receive the activity.</param>
        /// <param name="cancellationToken">CancellationToken propagates notifications that operations should be cancelled.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        private async Task SendToSkillAsync(ITurnContext turnContext, string deliveryMode, BotFrameworkSkill targetSkill, CancellationToken cancellationToken)
        {
            // NOTE: Always SaveChanges() before calling a skill so that any activity generated by the skill
            // will have access to current accurate state.
            await _conversationState.SaveChangesAsync(turnContext, force: true, cancellationToken: cancellationToken);

            // Route the activity to the skill.
            using var client = _auth.CreateBotFrameworkClient();

            // Create a conversationId to interact with the skill and send the activity
            var options = new SkillConversationIdFactoryOptions
            {
                FromBotOAuthScope = turnContext.TurnState.Get<string>(BotAdapter.OAuthScopeKey),
                FromBotId = _botId,
                Activity = turnContext.Activity,
                BotFrameworkSkill = targetSkill
            };

            var skillConversationId = await _conversationIdFactory.CreateSkillConversationIdAsync(options, cancellationToken);

            if (deliveryMode == DeliveryModes.ExpectReplies)
            {
                // Clone activity and update its delivery mode.
                var activity = JsonConvert.DeserializeObject<Activity>(JsonConvert.SerializeObject(turnContext.Activity));
                activity.DeliveryMode = deliveryMode;

                // route the activity to the skill
                var expectRepliesResponse = await client.PostActivityAsync(_botId, targetSkill.AppId, targetSkill.SkillEndpoint, _skillsConfig.SkillHostEndpoint, skillConversationId, activity, cancellationToken);

                // Check response status.
                if (!expectRepliesResponse.IsSuccessStatusCode())
                {
                    throw new HttpRequestException($"Error invoking the skill id: \"{targetSkill.Id}\" at \"{targetSkill.SkillEndpoint}\" (status is {expectRepliesResponse.Status}). \r\n {expectRepliesResponse.Body}");
                }

                // Route response activities back to the channel.
                var response = expectRepliesResponse.Body as JObject;
                var activities = response["activities"];
                var responseActivities = activities.ToObject<IList<Activity>>();

                foreach (var responseActivity in responseActivities)
                {
                    if (responseActivity.Type == ActivityTypes.EndOfConversation)
                    {
                        await EndConversation(responseActivity, turnContext, cancellationToken);
                    }
                    else
                    {
                        await turnContext.SendActivityAsync(responseActivity, cancellationToken);
                    }
                }
            }
            else
            {
                // Route the activity to the skill.
                var response = await client.PostActivityAsync(_botId, targetSkill.AppId, targetSkill.SkillEndpoint, _skillsConfig.SkillHostEndpoint, skillConversationId, turnContext.Activity, cancellationToken);

                // Check response status
                if (!response.IsSuccessStatusCode())
                {
                    throw new HttpRequestException($"Error invoking the skill id: \"{targetSkill.Id}\" at \"{targetSkill.SkillEndpoint}\" (status is {response.Status}). \r\n {response.Body}");
                }
            }
        }
    }
}
