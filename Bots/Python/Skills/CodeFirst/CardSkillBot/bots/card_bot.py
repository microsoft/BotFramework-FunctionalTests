# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import json
import os
import platform

from botbuilder.core import ActivityHandler, CardFactory, MessageFactory, TurnContext, ConversationState, UserState
from botbuilder.dialogs import Dialog
from botbuilder.schema import Activity, ActivityTypes, Attachment, EndOfConversationCodes
from helpers.dialog_helper import DialogHelper

PLATFORM = platform.system()
WINDOWS = "Windows"

class CardBot(ActivityHandler):
    async def on_message_activity(self, turn_context: TurnContext):
        if turn_context.activity.text:
            text = turn_context.activity.text.lower()
            if "end" in text or "stop" in text:
                # Send End of conversation at the end.
                await turn_context.send_activity(
                    MessageFactory.text("Ending conversation from the skill...")
                )

                end_of_conversation = Activity(type=ActivityTypes.end_of_conversation)
                end_of_conversation.code = EndOfConversationCodes.completed_successfully
                await turn_context.send_activity(end_of_conversation)
            elif text == "botaction":
                await self._send_adaptive_card(turn_context, "botaction")
            elif text == "taskmodule":
                await self._send_adaptive_card(turn_context, "taskmodule")
            elif text == "submit":
                await self._send_adaptive_card(turn_context, "submit")
            elif text == "hero":
                await self._send_hero_card(turn_context)
            else:
                await turn_context.send_activity(
                    MessageFactory.text(f"Send me botaction, taskmodule, submit, or hero for the respective card.")
                )
                await turn_context.send_activity(
                    MessageFactory.text(
                        f'Say "end" or "stop" and I\'ll end the conversation and back to the parent.'
                    )
                )
        else:
            await turn_context.send_activity(MessageFactory.text(f"The value of the activity was {turn_context.activity.value}"))

    async def on_end_of_conversation_activity(self, turn_context: TurnContext):
        # This will be called if the host bot is ending the conversation. Sending additional messages should be
        # avoided as the conversation may have been deleted.
        # Perform cleanup of resources if needed.
        pass

    async def _send_adaptive_card(self, turn_context: TurnContext, card_type: str):
        card = None
        card_path = ""
        if card_type == "botaction":
            if PLATFORM == WINDOWS:
                card_path = os.path.join(os.getcwd(), "cards\\bot_action.json")  
            else:
                card_path = os.path.join(os.getcwd(), "cards/bot_action.json") 
            with open(card_path, "rb") as in_file:
                card_data = json.load(in_file)
                card = CardFactory.adaptive_card(card_data)
        elif card_type == "taskmodule":
            if PLATFORM == WINDOWS:
                card_path = os.path.join(os.getcwd(), "cards\\task_module.json")  
            else:
                card_path = os.path.join(os.getcwd(), "cards/task_module.json") 

            with open(card_path, "rb") as in_file:
                card_data = json.load(in_file)
                card = CardFactory.adaptive_card(card_data)
        elif card_type == "submit":
            if PLATFORM == WINDOWS:
                card_path = os.path.join(os.getcwd(), "cards\\submit_action.json")  
            else:
                card_path = os.path.join(os.getcwd(), "cards/submit_action.json") 

            with open(card_path, "rb") as in_file:
                card_data = json.load(in_file)
                card = CardFactory.adaptive_card(card_data)
        else:
            raise Exception("Invalid card type. Must be botaction, taskmodule, submit, or hero.")
        reply_activity = MessageFactory.attachment(card)
        await turn_context.send_activity(reply_activity)

    async def _send_hero_card(self, turn_context: TurnContext):
        if PLATFORM == WINDOWS:
            card_path = os.path.join(os.getcwd(), "cards\\hero_card.json")  
        else:
            card_path = os.path.join(os.getcwd(), "cards/hero_card.json") 

        with open(card_path, "rb") as in_file:
            card_data = json.load(in_file)
        await turn_context.send_activity(
            MessageFactory.attachment(
                Attachment(
                    content_type=CardFactory.content_types.hero_card, content=card_data
                )
            )
        )