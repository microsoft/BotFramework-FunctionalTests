# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from botbuilder.core import ActivityHandler, MessageFactory, TurnContext, ConversationState, UserState
from botbuilder.dialogs import Dialog
from botbuilder.schema import Activity, ActivityTypes, EndOfConversationCodes
from helpers.dialog_helper import DialogHelper

class EchoBot(ActivityHandler):

    def __init__(
        self,
        conversation_state: ConversationState,
        user_state: UserState,
        dialog: Dialog,
    ):
        if conversation_state is None:
            raise Exception(
                "[DialogBot]: Missing parameter. conversation_state is required"
            )
        if user_state is None:
            raise Exception("[DialogBot]: Missing parameter. user_state is required")
        if dialog is None:
            raise Exception("[DialogBot]: Missing parameter. dialog is required")

        self.conversation_state = conversation_state
        self.user_state = user_state
        self.dialog = dialog

    async def on_message_activity(self, turn_context: TurnContext):
        if "auth" in turn_context.activity.text or "yes" in turn_context.activity.text:
            await DialogHelper.run_dialog(
                self.dialog,
                turn_context,
                self.conversation_state.create_property("DialogState"),
            )
        elif "end" in turn_context.activity.text or "stop" in turn_context.activity.text:
            # Send End of conversation at the end.
            await turn_context.send_activity(
                MessageFactory.text("Ending conversation from the skill...")
            )

            end_of_conversation = Activity(type=ActivityTypes.end_of_conversation)
            end_of_conversation.code = EndOfConversationCodes.completed_successfully
            await turn_context.send_activity(end_of_conversation)
        else:
            await turn_context.send_activity(
                MessageFactory.text(f"Echo: {turn_context.activity.text}")
            )
            await turn_context.send_activity(
                MessageFactory.text(
                    f'Say "end" or "stop" and I\'ll end the conversation and back to the parent.'
                )
            )

    async def on_end_of_conversation_activity(self, turn_context: TurnContext):
        # This will be called if the host bot is ending the conversation. Sending additional messages should be
        # avoided as the conversation may have been deleted.
        # Perform cleanup of resources if needed.
        pass

    async def on_turn(self, turn_context: TurnContext):
        await super().on_turn(turn_context)

        # Save any state changes that might have occurred during the turn.
        await self.conversation_state.save_changes(turn_context, False)
        await self.user_state.save_changes(turn_context, False)

    async def on_token_response_event(self, turn_context: TurnContext):
        # Run the Dialog with the new Token Response Event Activity.
        await DialogHelper.run_dialog(
            self.dialog,
            turn_context,
            self.conversation_state.create_property("DialogState"),
        )
