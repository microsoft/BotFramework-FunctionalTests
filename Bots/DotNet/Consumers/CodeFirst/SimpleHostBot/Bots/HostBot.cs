// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core.Skills;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Microsoft.BotFrameworkFunctionalTests.SimpleHostBot.Bots
{
    public class HostBot : ActivityHandler
    {
        public const string ActiveSkillPropertyName = "activeSkillProperty";
        
        // We use a single skill in this example.
        public const string TargetSkillId = "EchoSkillBot";
        
        private readonly IStatePropertyAccessor<BotFrameworkSkill> _activeSkillProperty;
        private readonly string _botId;
        private readonly ConversationState _conversationState;
        private readonly SkillHttpClient _skillClient;
        private readonly SkillsConfiguration _skillsConfig;
        private readonly BotFrameworkSkill _targetSkill;

        /// <summary>
        /// Initializes a new instance of the <see cref="HostBot"/> class.
        /// </summary>
        /// <param name="conversationState">A state management object for the conversation.</param>
        /// <param name="skillsConfig">The skills configuration.</param>
        /// <param name="skillClient">The HTTP client for the skills.</param>
        /// <param name="configuration">The configuration properties.</param>
        public HostBot(ConversationState conversationState, SkillsConfiguration skillsConfig, SkillHttpClient skillClient, IConfiguration configuration)
        {
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _skillsConfig = skillsConfig ?? throw new ArgumentNullException(nameof(skillsConfig));
            _skillClient = skillClient ?? throw new ArgumentNullException(nameof(skillClient));
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _botId = configuration.GetSection(MicrosoftAppCredentials.MicrosoftAppIdKey)?.Value;
            if (string.IsNullOrWhiteSpace(_botId))
            {
                throw new ArgumentException($"{MicrosoftAppCredentials.MicrosoftAppIdKey} is not set in configuration");
            }

            if (!_skillsConfig.Skills.TryGetValue(TargetSkillId, out _targetSkill))
            {
                throw new ArgumentException($"Skill with ID \"{TargetSkillId}\" not found in configuration");
            }

            // Create state property to track the active skill
            _activeSkillProperty = conversationState.CreateProperty<BotFrameworkSkill>(ActiveSkillPropertyName);
        }

        /// <summary>
        /// Processes a message activity.
        /// </summary>
        /// <param name="turnContext">Context for the current turn of conversation.</param>
        /// <param name="cancellationToken">CancellationToken propagates notifications that operations should be cancelled.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // Try to get the active skill
            var activeSkill = await _activeSkillProperty.GetAsync(turnContext, () => null, cancellationToken);

            if (activeSkill != null)
            {
                // Send the activity to the skill
                await SendToSkillAsync(turnContext, activeSkill, cancellationToken);
                return;
            }

            if (turnContext.Activity.Text.ToLower().Contains("skill"))
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Got it, connecting you to the skill..."), cancellationToken);

                // Save active skill in state
                await _activeSkillProperty.SetAsync(turnContext, _targetSkill, cancellationToken);

                // Send the activity to the skill
                await SendToSkillAsync(turnContext, _targetSkill, cancellationToken);
                return;
            }

            // just respond
            await turnContext.SendActivityAsync(MessageFactory.Text("Me no nothin'. Say \"skill\" and I'll patch you through"), cancellationToken);

            // Save conversation state
            await _conversationState.SaveChangesAsync(turnContext, force: true, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Processes an end of conversation activity.
        /// </summary>
        /// <param name="turnContext">Context for the current turn of conversation.</param>
        /// <param name="cancellationToken">CancellationToken propagates notifications that operations should be cancelled.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        protected override async Task OnEndOfConversationActivityAsync(ITurnContext<IEndOfConversationActivity> turnContext, CancellationToken cancellationToken)
        {
            // forget skill invocation
            await _activeSkillProperty.DeleteAsync(turnContext, cancellationToken);

            // Show status message, text and value returned by the skill
            var eocActivityMessage = $"Received {ActivityTypes.EndOfConversation}.\n\nCode: {turnContext.Activity.Code}";
            if (!string.IsNullOrWhiteSpace(turnContext.Activity.Text))
            {
                eocActivityMessage += $"\n\nText: {turnContext.Activity.Text}";
            }

            if ((turnContext.Activity as Activity)?.Value != null)
            {
                eocActivityMessage += $"\n\nValue: {JsonConvert.SerializeObject((turnContext.Activity as Activity)?.Value)}";
            }

            await turnContext.SendActivityAsync(MessageFactory.Text(eocActivityMessage), cancellationToken);

            // We are back at the host
            await turnContext.SendActivityAsync(MessageFactory.Text("Back in the host bot. Say \"skill\" and I'll patch you through"), cancellationToken);

            // Save conversation state
            await _conversationState.SaveChangesAsync(turnContext, cancellationToken: cancellationToken);
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
                }
            }
        }

        /// <summary>
        /// Sends an activity to the skill bot.
        /// </summary>
        /// <param name="turnContext">Context for the current turn of conversation.</param>
        /// <param name="targetSkill">The skill that will receive the activity.</param>
        /// <param name="cancellationToken">CancellationToken propagates notifications that operations should be cancelled.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        private async Task SendToSkillAsync(ITurnContext<IMessageActivity> turnContext, BotFrameworkSkill targetSkill, CancellationToken cancellationToken)
        {
            // NOTE: Always SaveChanges() before calling a skill so that any activity generated by the skill
            // will have access to current accurate state.
            await _conversationState.SaveChangesAsync(turnContext, force: true, cancellationToken: cancellationToken);

            // route the activity to the skill
            var response = await _skillClient.PostActivityAsync(_botId, targetSkill, _skillsConfig.SkillHostEndpoint, (Activity)turnContext.Activity, cancellationToken);

            // Check response status
            if (!(response.Status >= 200 && response.Status <= 299))
            {
                throw new HttpRequestException($"Error invoking the skill id: \"{targetSkill.Id}\" at \"{targetSkill.SkillEndpoint}\" (status is {response.Status}). \r\n {response.Body}");
            }
        }
    }
}
