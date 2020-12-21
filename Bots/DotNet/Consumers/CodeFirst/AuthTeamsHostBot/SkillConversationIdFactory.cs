// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.BotFrameworkFunctionalTests.SimpleHostBot
{
    /// <summary>
    /// A <see cref="SkillConversationIdFactory"/> that uses an in memory <see cref="ConcurrentDictionary{TKey,TValue}"/>
    /// to store and retrieve <see cref="ConversationReference"/> instances.
    /// </summary>
    public class SkillConversationIdFactory : SkillConversationIdFactoryBase
    {
        private readonly ConcurrentDictionary<string, string> _conversationRefs = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Creates a skill conversation id.
        /// </summary>
        /// <param name="conversationReference">The reference to a particular point of the conversation.</param>
        /// <param name="cancellationToken">CancellationToken propagates notifications that operations should be cancelled.</param>
        /// <returns>The generated conversation id.</returns>
        public override Task<string> CreateSkillConversationIdAsync(ConversationReference conversationReference, CancellationToken cancellationToken)
        {
            var crJson = JsonConvert.SerializeObject(conversationReference);
            var key = $"{conversationReference.ChannelId}:{conversationReference.Conversation.Id}";
            _conversationRefs.GetOrAdd(key, crJson);
            return Task.FromResult(key);
        }

        /// <summary>
        /// Gets the corresponding conversation reference of a conversation.
        /// </summary>
        /// <param name="skillConversationId">The id that identifies the skill conversation.</param>
        /// <param name="cancellationToken">CancellationToken propagates notifications that operations should be cancelled.</param>
        /// <returns>The generated conversation reference.</returns>
        public override Task<ConversationReference> GetConversationReferenceAsync(string skillConversationId, CancellationToken cancellationToken)
        {
            var conversationReference = JsonConvert.DeserializeObject<ConversationReference>(_conversationRefs[skillConversationId]);
            return Task.FromResult(conversationReference);
        }

        /// <summary>
        /// Deletes the conversation reference of a conversation.
        /// </summary>
        /// <param name="skillConversationId">The id that identifies the skill conversation.</param>
        /// <param name="cancellationToken">CancellationToken propagates notifications that operations should be cancelled.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public override Task DeleteConversationReferenceAsync(string skillConversationId, CancellationToken cancellationToken)
        {
            _conversationRefs.TryRemove(skillConversationId, out _);
            return Task.CompletedTask;
        }
    }
}
