# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from botbuilder.core import ActivityHandler, MessageFactory, TurnContext, ConversationState, UserState
from botbuilder.dialogs import Dialog
from botbuilder.schema import Activity, ActivityTypes, EndOfConversationCodes, ConversationReference
from helpers.dialog_helper import DialogHelper

class ProactiveBot(ActivityHandler):

    def __init__(
        self,
        conversation_references: ConversationReference
    ):
        if conversation_references is None:
            raise Exception(
                "[DialogBot]: Missing parameter. conversation_references is required"
            )
        
        self.conversation_references = conversation_references

    async def on_message_activity(self, turn_context: TurnContext):
        if "end" in turn_context.activity.text or "stop" in turn_context.activity.text:
            # Send End of conversation at the end.
            await turn_context.send_activity(
                MessageFactory.text("Ending conversation from the skill...")
            )

            end_of_conversation = Activity(type=ActivityTypes.end_of_conversation)
            end_of_conversation.code = EndOfConversationCodes.completed_successfully
            await turn_context.send_activity(end_of_conversation)
        else:
            self._add_conversation_reference(turn_context.activity)
            await turn_context.send_activity(
                MessageFactory.text(f"Visit http://localhost:39783/api/notify to proactively message all users who have messaged this bot.")
            )
            await turn_context.send_activity(
                MessageFactory.text(
                    f'Say "end" or "stop" and I\'ll end the conversation and back to the parent.'
                )
            )

    def _add_conversation_reference(self, activity: Activity):
        """
        This populates the shared Dictionary that holds conversation references. In this sample,
        this dictionary is used to send a message to members when /api/notify is hit.
        :param activity:
        :return:
        """
        conversation_reference = TurnContext.get_conversation_reference(activity)
        self.conversation_references[
            conversation_reference.user.id
        ] = conversation_reference

    async def on_end_of_conversation_activity(self, turn_context: TurnContext):
        # This will be called if the host bot is ending the conversation. Sending additional messages should be
        # avoided as the conversation may have been deleted.
        # Perform cleanup of resources if needed.
        pass
