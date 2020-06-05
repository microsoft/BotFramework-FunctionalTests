// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.BotFrameworkFunctionalTests.EchoSkillBot.OAuth;

namespace Microsoft.BotFrameworkFunctionalTests.EchoSkillBot.Bots
{
    public class EchoBot : ActivityHandler
    {
        private readonly LoginDialog loginDialog;
        private readonly ConversationState conversationState;

        public EchoBot(ConversationState conversationState, LoginDialog loginDialog)
        {
            this.conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            this.loginDialog = loginDialog ?? throw new ArgumentNullException(nameof(loginDialog));
        }

        /// <summary>
        /// Processes a message activity.
        /// </summary>
        /// <param name="turnContext">Context for the current turn of conversation.</param>
        /// <param name="cancellationToken">CancellationToken propagates notifications that operations should be cancelled.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var text = turnContext.Activity.Text;
            if (text.Contains("auth") || text.Contains("logout") || text.Contains("Yes") || text.Contains("No"))
            {
                await loginDialog.RunAsync(turnContext, conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
                
                // Save any state changes that might have occurred during the turn.
                await conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            }
            else if (text.Contains("end") || text.Contains("stop"))
            {
                // Send End of conversation at the end.
                await turnContext.SendActivityAsync(MessageFactory.Text($"ending conversation from the skill..."), cancellationToken);
                var endOfConversation = Activity.CreateEndOfConversationActivity();
                endOfConversation.Code = EndOfConversationCodes.CompletedSuccessfully;
                await turnContext.SendActivityAsync(endOfConversation, cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text($"Echo: {text}"), cancellationToken);
                await turnContext.SendActivityAsync(MessageFactory.Text("Say \"end\" or \"stop\" and I'll end the conversation and back to the parent."), cancellationToken);
            }
        }

        /// <summary>
        /// Processes an end of conversation activity.
        /// </summary>
        /// <param name="turnContext">Context for the current turn of conversation.</param>
        /// <param name="cancellationToken">CancellationToken propagates notifications that operations should be cancelled.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        protected override Task OnEndOfConversationActivityAsync(ITurnContext<IEndOfConversationActivity> turnContext, CancellationToken cancellationToken)
        {
            // This will be called if the host bot is ending the conversation. Sending additional messages should be
            // avoided as the conversation may have been deleted.
            // Perform cleanup of resources if needed.
            return Task.CompletedTask;
        }

        protected override async Task OnTokenResponseEventAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            // Run the Dialog with the new Token Response Event Activity.
            await loginDialog.RunAsync(turnContext, conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
        }

    }
}
