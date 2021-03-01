# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import os
import base64
from botbuilder.core import BotAdapter, MessageFactory
from botbuilder.dialogs import (
    ComponentDialog,
    ChoicePrompt,
    DialogTurnResult,
    DialogTurnStatus,
    WaterfallDialog,
    WaterfallStepContext,
    Choice,
)
from botbuilder.dialogs.prompts import PromptOptions
from botbuilder.schema import (
    Attachment,
    AttachmentData,
    Activity,
    ActivityTypes,
    EndOfConversationCodes,
    InputHints,
)


ATTACHMENT_TYPE_PROMPT = "AttachmentTypePrompt"
END_PROMPT = "EndPrompt"


class MessageWithAttachmentDialog(ComponentDialog):
    def __init__(self):
        super().__init__(MessageWithAttachmentDialog.__name__)

        self.picture = "architecture-resize.png"

        self.add_dialog(ChoicePrompt(ATTACHMENT_TYPE_PROMPT))
        self.add_dialog(ChoicePrompt(END_PROMPT))
        self.add_dialog(
            WaterfallDialog(
                WaterfallDialog.__name__,
                [self.select_attachment, self.handle_attachment, self.final_step],
            )
        )

        self.initial_dialog_id = WaterfallDialog.__name__

    async def select_attachment(self, step_context: WaterfallStepContext):
        # Create the PromptOptions from the skill configuration which contain the list of configured skills.
        message_text = "What card do you want?"
        reprompt_message_text = (
            "That was not a valid choice, please select a valid card type."
        )

        options = PromptOptions(
            prompt=MessageFactory.text(
                message_text, message_text, InputHints.expecting_input
            ),
            retry_prompt=MessageFactory.text(
                reprompt_message_text, reprompt_message_text, InputHints.expecting_input
            ),
            choices=[Choice("Inline"), Choice("Internet")]
            # This is currently excluded since Attachments endpoint
            # isn't currently implemented in the ChannelServiceHandler.
            # Choice('Upload')
        )

        return await step_context.prompt(ATTACHMENT_TYPE_PROMPT, options)

    async def handle_attachment(self, step_context: WaterfallStepContext):
        card = step_context.context.activity.text
        reply = MessageFactory.text("")

        if card == "Inline":
            reply.text = "This is an inline attachment."
            reply.attachments = [await self.get_inline_attachment()]

        elif card == "Internet":
            reply.text = "This is an attachment from a HTTP URL."
            reply.attachments = [await self.get_internet_attachment()]

        # elif card == 'Upload':
        #     # Commenting this out since the Attachments endpoint
        #     # isn't currently implemented in the ChannelService Handler

        #     reply.text = 'This is an uploaded attachment.'
        #     # Get the uploaded attachment.
        #     uploaded_attachment = await self.get_uploaded_attachment(
        #         step_context,
        #         step_context.context.activity.service_url,
        #         step_context.context.activity.conversation.id
        #     )
        #     reply.attachments = [uploaded_attachment]

        else:
            reply.text = "Invalid choice"

        await step_context.context.send_activity(reply)

        message_text = "Do you want another type of attachment?"
        reprompt_message_text = "That's an invalid choice."

        options = PromptOptions(
            prompt=MessageFactory.text(
                message_text, message_text, InputHints.expecting_input
            ),
            retry_prompt=MessageFactory.text(
                reprompt_message_text, reprompt_message_text, InputHints.expecting_input
            ),
            choices=[Choice("Yes"), Choice("No")],
        )

        return await step_context.prompt(END_PROMPT, options)

    async def final_step(self, step_context: WaterfallStepContext):
        selected_choice = str(step_context.result.value).lower()

        if selected_choice == "yes":
            return await step_context.replace_dialog(self.initial_dialog_id)

        await step_context.context.send_activity(
            Activity(
                type=ActivityTypes.end_of_conversation,
                code=EndOfConversationCodes.completed_successfully,
            )
        )
        return DialogTurnResult(DialogTurnStatus.Complete)

    async def get_inline_attachment(self):
        file_path = os.path.join(
            os.getcwd(), "dialogs", "message_with_attachment", "files", self.picture
        )

        with open(file_path, "rb") as in_file:
            file = base64.b64encode(in_file.read()).decode()

        return Attachment(
            name=f"Files\\{ self.picture }",
            content_type="image/png",
            content_url=f"data:image/png;base64,{file}",
        )

    async def get_internet_attachment(self):
        return Attachment(
            name=f"Files\\{ self.picture }",
            content_type="image/png",
            content_url="https://docs.microsoft.com/en-us/bot-framework/media/how-it-works/architecture-resize.png",
        )

    async def get_uploaded_attachment(
        self, step_context: WaterfallStepContext, service_url: str, conversation_id: str
    ):
        if service_url is None:
            raise Exception(
                "[MessageWithAttachmentDialog]: Missing parameter. service_url is required"
            )
        if service_url is None:
            raise Exception(
                "[MessageWithAttachmentDialog]: Missing parameter. conversation_id is required"
            )

        file_path = os.path.join(
            os.getcwd(), "dialogs", "message_with_attachment", "files", self.picture
        )

        with open(file_path, "rb") as in_file:
            file = in_file.read()

        connector = step_context.context.turn_state.get(
            BotAdapter.BOT_CONNECTOR_CLIENT_KEY
        )

        response = await connector.conversations.upload_attachment(
            conversation_id,
            AttachmentData(
                name=f"dialogs\\message_with_attachment\\files\\{ self.picture }",
                type="image/png",
                original_base64=file,
            ),
        )

        base_uri: str = connector.config.base_url
        attachment_uri = (
            base_uri
            + ("" if base_uri.endswith("/") else "/")
            + f"v3/attachments/{response.id}/views/original"
        )

        return Attachment(
            name=f"dialogs\\message_with_attachment\\files\\{ self.picture }",
            content_type="image/png",
            content_url=attachment_uri,
        )
