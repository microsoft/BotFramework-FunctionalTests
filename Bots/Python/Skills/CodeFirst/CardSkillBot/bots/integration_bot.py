# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from datetime import datetime
import os
import json
import requests
import xmlrpc.client
import pprint
import pkg_resources
import time
from botbuilder.core import CardFactory, TurnContext, MessageFactory

from botbuilder.core.teams import (
    TeamsActivityHandler,
    TeamsInfo,
)

from botbuilder.schema import (
    Activity,
    ActivityTypes,
    Attachment,
    CardAction,
    ChannelAccount,
    ConversationAccount,
    ConversationParameters,
    HeroCard,
    Mention,
    MessageReaction,
)

from botbuilder.schema.teams import (
    ChannelInfo,
    FileDownloadInfo,
    FileConsentCard,
    FileConsentCardResponse,
    FileInfoCard,
    TeamsChannelAccount,
    TeamsChannelData,  # TODO: https://github.com/microsoft/botbuilder-python/pull/1069
    TeamInfo,
    MessagingExtensionAction,
    MessagingExtensionActionResponse,
    TaskModuleContinueResponse,
    MessagingExtensionResult,
    TaskModuleTaskInfo,
    MessagingExtensionAttachment,
    MessagingExtensionQuery,
    MessagingExtensionResult,
    MessagingExtensionResponse,
)

from example_data import ExampleData

from adaptive_card_helper import (
    create_adaptive_card_editor,
    create_adaptive_card_preview,
)

from botbuilder.schema.teams.additional_properties import ContentType
from botbuilder.schema._connector_client_enums import ActionTypes
from activity_log import ActivityLog
from typing import List


class PythonIntegrationBot(TeamsActivityHandler):
    def __init__(self, app_id: str, app_password: str, log: ActivityLog):
        self._app_id = app_id
        self._app_password = app_password
        self._log = log
        self._activity_ids = []

    async def on_message_activity(self, turn_context: TurnContext):
        if turn_context.activity.text:
            TurnContext.remove_recipient_mention(turn_context.activity)
            turn_context.activity.text = turn_context.activity.text.strip().lower()
            text = turn_context.activity.text
            if text == "command:reset":
                await self._reset_bot(turn_context)
            elif text == "command:getsdkversions":
                await self._return_current_sdk_version(turn_context)
            elif text == "proactivent":
                await self._send_proactive_non_threaded_message(turn_context)
            elif text == "proactive":
                await self._send_proactive_threaded_message(turn_context)
            elif text == "delete":
                await self._delete_activity(turn_context)
            elif text == "update":
                await self._update_activity(turn_context)
            elif text == "1":
                await self._send_adaptive_card(turn_context, 1)
            elif text == "2":
                await self._send_adaptive_card(turn_context, 2)
            elif text == "3":
                await self._send_adaptive_card(turn_context, 3)
            elif text == "hero":
                await self._send_hero_card(turn_context)
            elif text == "thumbnail":
                await self._send_thumbnail_card(turn_context)
            elif text == "receipt":
                await self._send_receipt_card(turn_context)
            elif text == "signin":
                await self._send_signin_card(turn_context)
            elif text == "carousel":
                await self._send_carousel_card(turn_context)
            elif text == "list":
                await self._send_list_card(turn_context)
            elif text == "o365":
                await self._send_o365_card(turn_context)
            elif text == "file":
                filename = "teams-logo.png"
                file_path = "files/" + filename
                file_size = os.path.getsize(file_path)
                await self._send_file_card(turn_context, filename, file_size)
            elif text == "show members":
                await self._show_members(turn_context)
            elif text == "show channels":
                await self._show_channels(turn_context)
            elif text == "show details":
                await self._show_team_details(turn_context)
            elif text == "task module":
                await self._show_task_module(turn_context)
            elif text == "mention":
                await self._mention_activity(turn_context)
            elif text == "upload file":
                await self._show_upload_file(turn_context)
            else:
                await self._send_message_and_log_activity_id(
                    turn_context, f"You said: {turn_context.activity.text}"
                )
        else:
            await turn_context.send_activity(
                MessageFactory.text("App sent a message with empty text")
            )
            if turn_context.activity.value:
                await self._send_message_and_log_activity_id(
                    turn_context,
                    f"but with value {json.dumps(turn_context.activity.value)}",
                )

        return

    async def on_reactions_added(
        self, message_reactions: List[MessageReaction], turn_context: TurnContext
    ):
        for reaction in message_reactions:
            activity = await self._log.find(turn_context.activity.reply_to_id)
            if activity:
                await self._send_message_and_log_activity_id(
                    turn_context,
                    f"You added '{reaction.type}' regarding '{activity.text}'",
                )
            else:
                await self._send_message_and_log_activity_id(
                    turn_context,
                    f"Activity {turn_context.activity.reply_to_id} not found in the log.",
                )

    async def on_reactions_removed(
        self, message_reactions: List[MessageReaction], turn_context: TurnContext
    ):
        for reaction in message_reactions:
            activity = await self._log.find(turn_context.activity.reply_to_id)
            if activity:
                await self._send_message_and_log_activity_id(
                    turn_context,
                    f"You removed '{reaction.type}' regarding '{activity.text}'",
                )
            else:
                await self._send_message_and_log_activity_id(
                    turn_context,
                    f"Activity {turn_context.activity.reply_to_id} not found in the log.",
                )

    async def on_teams_members_removed(
        self, teams_members_removed: [TeamsChannelAccount], turn_context: TurnContext
    ):
        if not turn_context:
            raise Exception("turn_context cannot be null")

        # TODO: Update once https://github.com/microsoft/botbuilder-python/pull/1069 is resolved
        channel_data = TeamsChannelData().deserialize(
            turn_context.activity.channel_data
        )
        if channel_data:
            team_info = channel_data.team
            location = (
                team_info.name
                if team_info
                else turn_context.activity.conversation.conversation_type
            )
            hero_card = HeroCard(
                text=" ".join(
                    [
                        f"{member.id} removed from {location}"
                        for member in teams_members_removed
                    ]
                )
            )
            await turn_context.send_activity(
                MessageFactory.attachment(CardFactory.hero_card(hero_card))
            )

    async def on_teams_members_added(  # pylint: disable=unused-argument
        self,
        teams_members_added: [TeamsChannelAccount],
        team_info: TeamInfo,
        turn_context: TurnContext,
    ):
        if not turn_context:
            raise Exception("turn_context cannot be null")

        location = (
            team_info.name
            if team_info
            else turn_context.activity.conversation.conversation_type
        )
        hero_card = HeroCard(
            text=" ".join(
                [f"{member.id} joined {location}" for member in teams_members_added]
            )
        )
        await turn_context.send_activity(
            MessageFactory.attachment(CardFactory.hero_card(hero_card))
        )

    async def _create_with_preview(
        self, turn_context: TurnContext, action: MessagingExtensionAction
    ):
        preview_card = create_adaptive_card_preview(
            user_text=action.data["Question"],
            is_multi_select=action.data["MultiSelect"],
            option1=action.data["Option1"],
            option2=action.data["Option2"],
            option3=action.data["Option3"],
        )

        extension_result = MessagingExtensionResult(
            type="botMessagePreview",
            activity_preview=MessageFactory.attachment(preview_card),
        )
        return MessagingExtensionActionResponse(compose_extension=extension_result)

    async def _send_hero_card(self, turn_context: TurnContext):
        card_path = os.path.join(os.getcwd(), "cards\\hero_card.json")
        with open(card_path, "rb") as in_file:
            card_data = json.load(in_file)
        await turn_context.send_activity(
            MessageFactory.attachment(
                Attachment(
                    content_type=CardFactory.content_types.hero_card, content=card_data
                )
            )
        )

    async def _send_thumbnail_card(self, turn_context: TurnContext):
        card_path = os.path.join(os.getcwd(), "cards\\thumbnail_card.json")
        with open(card_path, "rb") as in_file:
            card_data = json.load(in_file)
        await turn_context.send_activity(
            MessageFactory.attachment(
                Attachment(
                    content_type=CardFactory.content_types.thumbnail_card,
                    content=card_data,
                )
            )
        )

    async def _send_receipt_card(self, turn_context: TurnContext):
        card_path = os.path.join(os.getcwd(), "cards\\receipt_card.json")
        with open(card_path, "rb") as in_file:
            card_data = json.load(in_file)
        await turn_context.send_activity(
            MessageFactory.attachment(
                Attachment(
                    content_type=CardFactory.content_types.receipt_card,
                    content=card_data,
                )
            )
        )

    async def _send_signin_card(self, turn_context: TurnContext):
        card_path = os.path.join(os.getcwd(), "cards\\signin_card.json")
        with open(card_path, "rb") as in_file:
            card_data = json.load(in_file)
        await turn_context.send_activity(
            MessageFactory.attachment(
                Attachment(
                    content_type=CardFactory.content_types.signin_card,
                    content=card_data,
                )
            )
        )
    
    async def _return_current_sdk_version(self, turn_context: TurnContext):
        version = pkg_resources.get_distribution("botbuilder-core").version
        await turn_context.send_activity(MessageFactory.text(f"{turn_context.activity.value} The bot version is {version}"))

    async def _reset_bot(self, turn_context):
        await self._log.delete(self._activity_ids)
        self._activity_ids = []
        await turn_context.send_activity(MessageFactory.text("I'm reset. Test away!"))

    async def _send_carousel_card(self, turn_context: TurnContext):
        card_path = os.path.join(os.getcwd(), "cards\\hero_card.json")
        with open(card_path, "rb") as in_file:
            card_data = json.load(in_file)
        attachment = Attachment(
            content_type=CardFactory.content_types.hero_card, content=card_data
        )
        await turn_context.send_activity(
            MessageFactory.carousel([attachment, attachment, attachment])
        )

    async def _show_task_module(self, turn_context: TurnContext):
        card_path = os.path.join(os.getcwd(), "cards\\task_module_hero_card.json")
        with open(card_path, "rb") as in_file:
            card_data = json.load(in_file)
        await turn_context.send_activity(MessageFactory.attachment(card_data))

    async def _send_list_card(self, turn_context: TurnContext):
        card_path = os.path.join(os.getcwd(), "cards\\hero_card.json")
        with open(card_path, "rb") as in_file:
            card_data = json.load(in_file)
        attachment = Attachment(
            content_type=CardFactory.content_types.hero_card, content=card_data
        )
        await turn_context.send_activity(
            MessageFactory.list([attachment, attachment, attachment])
        )

    async def _send_o365_card(self, turn_context: TurnContext):
        card_path = os.path.join(os.getcwd(), "cards\\o365_card.json")
        with open(card_path, "rb") as in_file:
            card_data = json.load(in_file)
        await turn_context.send_activity(
            MessageFactory.attachment(
                Attachment(
                    content_type="application/vnd.microsoft.teams.card.o365connector",
                    content=card_data,
                )
            )
        )

    async def _send_file_card(self, turn_context: TurnContext):
        card_path = os.path.join(os.getcwd(), "cards\\file_card.json")
        with open(card_path, "rb") as in_file:
            card_data = json.load(in_file)
        await turn_context.send_activity(MessageFactory.attachment(card_data))

    async def _create_card_command(
        self,
        turn_context: TurnContext,  # pylint: disable=unused-argument
        action: MessagingExtensionAction,
    ) -> MessagingExtensionActionResponse:
        title = action.data["title"]
        sub_title = action.data["subTitle"]
        text = action.data["text"]

        card = HeroCard(title=title, subtitle=sub_title, text=text)
        attachment = MessagingExtensionAttachment(
            content=card,
            content_type=CardFactory.content_types.hero_card,
            preview=CardFactory.hero_card(card),
        )
        attachments = [attachment]

        extension_result = MessagingExtensionResult(
            attachment_layout="list", type="result", attachments=attachments
        )
        return MessagingExtensionActionResponse(compose_extension=extension_result)

    async def _send_adaptive_card(self, turn_context: TurnContext, card_type: int):
        card = None
        if card_type == 1:
            card_path = card_path = os.path.join(os.getcwd(), "cards\\bot_action.json")
            with open(card_path, "rb") as in_file:
                card_data = json.load(in_file)
                card = CardFactory.adaptive_card(card_data)
        elif card_type == 2:
            card_path = card_path = os.path.join(os.getcwd(), "cards\\task_module.json")
            with open(card_path, "rb") as in_file:
                card_data = json.load(in_file)
                card = CardFactory.adaptive_card(card_data)
        elif card_type == 3:
            card_path = card_path = os.path.join(
                os.getcwd(), "cards\\submit_action.json"
            )
            with open(card_path, "rb") as in_file:
                card_data = json.load(in_file)
                card = CardFactory.adaptive_card(card_data)
        else:
            raise Exception("Invalid card type. Must be 1, 2 or 3.")
        reply_activity = MessageFactory.attachment(card)
        await turn_context.send_activity(reply_activity)

    async def _update_activity(self, turn_context: TurnContext):
        for activity_id in self._activity_ids:
            new_activity = MessageFactory.text(turn_context.activity.text)
            new_activity.id = activity_id
            await turn_context.update_activity(new_activity)
        return

    async def _share_message_command(
        self,
        turn_context: TurnContext,  # pylint: disable=unused-argument
        action: MessagingExtensionAction,
    ) -> MessagingExtensionActionResponse:
        # The user has chosen to share a message by choosing the 'Share Message' context menu command.
        title = f"{action.message_payload.from_property.user.display_name} orignally sent this message:"
        text = action.message_payload.body.content
        card = HeroCard(title=title, text=text)

        if not action.message_payload.attachments is None:
            # This sample does not add the MessagePayload Attachments.  This is left as an
            #  exercise for the user.
            card.subtitle = (
                f"({len(action.message_payload.attachments)} Attachments not included)"
            )

        # This Messaging Extension example allows the user to check a box to include an image with the
        # shared message.  This demonstrates sending custom parameters along with the message payload.
        include_image = action.data["includeImage"]
        if include_image == "true":
            image = CardImage(
                url="https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQtB3AwMUeNoq4gUBGe6Ocj8kyh3bXa9ZbV7u1fVKQoyKFHdkqU"
            )
            card.images = [image]

        attachment = MessagingExtensionAttachment(
            content=card,
            content_type=CardFactory.content_types.hero_card,
            preview=CardFactory.hero_card(card),
        )

        extension_result = MessagingExtensionResult(
            attachment_layout="list", type="result", attachments=[attachment]
        )
        return MessagingExtensionActionResponse(compose_extension=extension_result)

    async def on_teams_team_renamed_activity(  # pylint: disable=unused-argument
        self, team_info: TeamInfo, turn_context: TurnContext
    ):
        if not turn_context:
            raise Exception("turn_context cannot be null")

        if not team_info:
            raise Exception("team_info cannot be null")

        hero_card = HeroCard(text=f"{team_info.name} is the Team name")
        await turn_context.send_activity(
            MessageFactory.attachment(CardFactory.hero_card(hero_card))
        )

    async def on_teams_channel_deleted(  # pylint: disable=unused-argument
        self, channel_info: ChannelInfo, team_info: TeamInfo, turn_context: TurnContext
    ):
        if not turn_context:
            raise Exception("turn_context cannot be null")

        if not channel_info:
            raise Exception("channel_info cannot be null")

        if not team_info:
            raise Exception("team_info cannot be null")

        hero_card = HeroCard(text=f"{channel_info.name} is the Channel deleted")
        await turn_context.send_activity(
            MessageFactory.attachment(CardFactory.hero_card(hero_card))
        )

    async def on_teams_channel_created(  # pylint: disable=unused-argument
        self, channel_info: ChannelInfo, team_info: TeamInfo, turn_context: TurnContext
    ):
        if not turn_context:
            raise Exception("turn_context cannot be null")

        if not channel_info:
            raise Exception("channel_info cannot be null")

        if not team_info:
            raise Exception("team_info cannot be null")

        hero_card = HeroCard(text=f"{channel_info.name} is the Channel Created")
        await turn_context.send_activity(
            MessageFactory.attachment(CardFactory.hero_card(hero_card))
        )

    async def on_teams_messaging_extension_submit_action(  # pylint: disable=unused-argument
        self, turn_context: TurnContext, action: MessagingExtensionAction
    ) -> MessagingExtensionActionResponse:

        if action.command_id == "createCard":
            return await self._create_card_command(turn_context, action)
        if action.command_id == "shareMessage":
            return await self._share_message_command(turn_context, action)
        if action.command_id == "createWithPreview":
            return await self._create_with_preview(turn_context, action)

        # preview_card = create_adaptive_card_preview(
        #     user_text=action.data["Question"],
        #     is_multi_select=action.data["MultiSelect"],
        #     option1=action.data["Option1"],
        #     option2=action.data["Option2"],
        #     option3=action.data["Option3"],
        # )

        # extension_result = MessagingExtensionResult(
        #     type="botMessagePreview",
        #     activity_preview=MessageFactory.attachment(preview_card),
        # )
        return MessagingExtensionActionResponse(compose_extension=extension_result)

    async def on_teams_messaging_extension_fetch_task(
        self, turn_context: TurnContext, action: MessagingExtensionAction
    ) -> MessagingExtensionActionResponse:
        card = create_adaptive_card_editor()
        task_info = TaskModuleTaskInfo(
            card=card, height=450, title="Task Module Fetch Example", width=500
        )
        continue_response = TaskModuleContinueResponse(value=task_info)
        return MessagingExtensionActionResponse(task=continue_response)

    async def on_teams_messaging_extension_bot_message_preview_edit(  # pylint: disable=unused-argument
        self, turn_context: TurnContext, action: MessagingExtensionAction
    ) -> MessagingExtensionActionResponse:
        activity_preview = action.bot_activity_preview[0]
        content = activity_preview.attachments[0].content
        data = self._get_example_data(content)
        card = create_adaptive_card_editor(
            data.question,
            data.is_multi_select,
            data.option1,
            data.option2,
            data.option3,
        )
        task_info = TaskModuleTaskInfo(
            card=card, height=450, title="Task Module Fetch Example", width=500
        )
        continue_response = TaskModuleContinueResponse(value=task_info)
        return MessagingExtensionActionResponse(task=continue_response)

    async def on_teams_messaging_extension_bot_message_preview_send(  # pylint: disable=unused-argument
        self, turn_context: TurnContext, action: MessagingExtensionAction
    ) -> MessagingExtensionActionResponse:
        activity_preview = action.bot_activity_preview[0]
        content = activity_preview.attachments[0].content
        data = self._get_example_data(content)
        card = create_adaptive_card_preview(
            data.question,
            data.is_multi_select,
            data.option1,
            data.option2,
            data.option3,
        )

        message = MessageFactory.attachment(card)
        await turn_context.send_activity(message)

    async def on_teams_channel_renamed(  # pylint: disable=unused-argument
        self, channel_info: ChannelInfo, team_info: TeamInfo, turn_context: TurnContext
    ):
        if not turn_context:
            raise Exception("turn_context cannot be null")

        if not channel_info:
            raise Exception("channel_info cannot be null")

        hero_card = HeroCard(text=f"{channel_info.name} is the new Channel name")
        await turn_context.send_activity(
            MessageFactory.attachment(CardFactory.hero_card(hero_card))
        )

    async def on_teams_file_consent_accept(
        self,
        turn_context: TurnContext,
        file_consent_card_response: FileConsentCardResponse,
    ):

        """        
        The user accepted the file upload request.  Do the actual upload now.
        """     

        file_path = "files/" + file_consent_card_response.context["filename"]
        file_size = os.path.getsize(file_path)

        headers = {
            "Content-Length": f'"{file_size}"',
            "Content-Range": f"bytes 0-{file_size-1}/{file_size}",
        }
        response = requests.put(
            file_consent_card_response.upload_info.upload_url,
            open(file_path, "rb"),
            headers=headers,
        )

        if response.status_code != 201:
            print(
                f"Failed to upload, status {response.status_code}, file_path={file_path}"
            )
            await self._file_upload_failed(turn_context, "Unable to upload file.")
        else:
            await self._file_upload_complete(turn_context, file_consent_card_response)

    async def on_teams_file_consent_decline(
        self,
        turn_context: TurnContext,
        file_consent_card_response: FileConsentCardResponse,
    ):

        """
        #The user declined the file upload.
        """

        context = file_consent_card_response.context

        reply = self._create_reply(
            turn_context.activity,
            f"Declined. We won't upload file <b>{context['filename']}</b>.",
            "xml",
        )
        await turn_context.send_activity(reply)

    async def on_teams_messaging_extension_query(
        self, turn_context: TurnContext, query: MessagingExtensionQuery
    ):
        search_query = str(query.parameters[0].value).strip()
        if search_query == "":
            await turn_context.send_activity(
                MessageFactory.text("You cannot enter a blank string for the search")
            )
            return

        search_results = self._get_search_results(search_query)

        attachments = []
        for obj in search_results:
            hero_card = HeroCard(
                title=obj["name"], tap=CardAction(type="invoke", value=obj)
            )

            attachment = MessagingExtensionAttachment(
                content_type=CardFactory.content_types.hero_card,
                content=HeroCard(title=obj["name"]),
                preview=CardFactory.hero_card(hero_card),
            )
            attachments.append(attachment)
        return MessagingExtensionResponse(
            compose_extension=MessagingExtensionResult(
                type="result", attachment_layout="list", attachments=attachments
            )
        )

    async def on_teams_messaging_extension_select_item(
        self, turn_context: TurnContext, query
    ) -> MessagingExtensionResponse:
        hero_card = HeroCard(
            title=query["name"],
            subtitle=query["summary"],
            buttons=[
                CardAction(
                    type="openUrl", value=f"https://pypi.org/project/{query['name']}"
                )
            ],
        )
        attachment = MessagingExtensionAttachment(
            content_type=CardFactory.content_types.hero_card, content=hero_card
        )

        return MessagingExtensionResponse(
            compose_extension=MessagingExtensionResult(
                type="result", attachment_layout="list", attachments=[attachment]
            )
        )

    def _get_example_data(self, content: dict) -> ExampleData:
        body = content["body"]
        question = body[1]["text"]
        choice_set = body[3]
        multi_select = "isMultiSelect" in choice_set
        option1 = choice_set["choices"][0]["value"]
        option2 = choice_set["choices"][1]["value"]
        option3 = choice_set["choices"][2]["value"]
        return ExampleData(question, multi_select, option1, option2, option3)

    def _get_search_results(self, query: str):
        client = xmlrpc.client.ServerProxy("https://pypi.org/pypi")
        search_results = client.search({"name": query})
        return search_results[:10] if len(search_results) > 10 else search_results

    async def _show_members(self, turn_context):
        members = await TeamsInfo.get_members(turn_context)
        reply_activity = MessageFactory.text(
            f"Total of {len(members)} members are currently in the team"
        )
        messages = [
            f"{member.aad_object_id} --> {member.name} --> {member.user_principal_name}"
            for member in members
        ]

        await self._send_in_batches(turn_context, messages)

    
    async def _show_channels(self, turn_context: TurnContext):
        team_id = TeamsInfo.get_team_id(turn_context)
        if team_id:
            channels = await TeamsInfo.get_team_channels(turn_context, team_id)

            reply_activity = MessageFactory.text(
                f"Total of {len(channels)} channels are currently in team"
            )
            await turn_context.send_activity(reply_activity)

            messages = [f"{channel.id} --> {channel.name}" for channel in channels]

            await self._send_in_batches(turn_context, messages)
        else:
            await self._send_message_and_log_activity_id(turn_context, "This only works in the team scope")

    async def _send_in_batches(self, turn_context: TurnContext, messages: List[str]):
        batch = []
        for msg in messages:
            batch.append(msg)
            if len(batch) == 10:
                await self._send_message_and_log_activity_id(
                    turn_context, "<br>".join(batch)
                )
                batch.clear()

        if len(batch) > 0:
            await self._send_message_and_log_activity_id(
                turn_context, "<br>".join(batch)
            )

    async def _show_team_details(self, turn_context: TurnContext):
        team_id = TeamsInfo.get_team_id(turn_context)
        if team_id:
            team_details = await TeamsInfo.get_team_details(turn_context, team_id)
            await self._send_message_and_log_activity_id(
                turn_context,
                f"The team name is {team_details.name}. The team ID is {team_details.id}. The ADD Group Id is {team_details.aad_group_id}.",
            )
        else:
            await self._send_message_and_log_activity_id(turn_context, "This only works in the team scope")

    async def _send_message_and_log_activity_id(
        self, turn_context: TurnContext, text: str
    ):
        reply_activity = MessageFactory.text(text)
        resource_response = await turn_context.send_activity(reply_activity)
        await self._log.append(resource_response.id, reply_activity)
        self._activity_ids.append(resource_response.id)

    async def _show_card(self, turn_context: TurnContext):
        card = HeroCard(
            title="Welcome Card",
            text="Click the buttons to update this card",
            buttons=[
                CardAction(
                    type=ActionTypes.message_back,
                    title="Update Card",
                    text="UpdateCardAction",
                    value={"count": 0},
                ),
                CardAction(
                    type=ActionTypes.message_back,
                    title="Message all memebers",
                    text="MessageAllMembers",
                ),
            ],
        )
        await turn_context.send_activity(
            MessageFactory.attachment(CardFactory.hero_card(card))
        )

    async def _show_upload_file(self, turn_context: TurnContext):
        message_with_file_download = (
            False
            if not turn_context.activity.attachments
            else turn_context.activity.attachments[0].content_type
            == ContentType.FILE_DOWNLOAD_INFO
        )

        if message_with_file_download:
            file = turn_context.activity.attachments[0]
            file_download = FileDownloadInfo.deserialize(file.content)
            file_path = "files/" + file.name

            response = requests.get(file_download.download_url, allow_redirects=True)
            open(file_path, "wb").write(response.content)

            reply = self._create_reply(
                turn_context.activity, f"Complete downloading <b>{file.name}</b>", "xml"
            )
            await turn_context.send_activity(reply)
        else:
            filename = "teams-logo.png"
            file_path = "files/" + filename
            file_size = os.path.getsize(file_path)
            await self._send_file_card(turn_context, filename, file_size)

    async def _mention_activity(self, turn_context: TurnContext):
        mention = Mention(
            mentioned=turn_context.activity.from_property,
            text=f"<at>{turn_context.activity.from_property.name}</at>",
            type="mention",
        )

        reply_activity = MessageFactory.text(f"Hello {mention.text}")
        reply_activity.entities = [Mention().deserialize(mention.serialize())]
        await turn_context.send_activity(reply_activity)

    async def _update_card_activity(self, turn_context: TurnContext):
        data = turn_context.activity.value
        data["count"] += 1

        card = CardFactory.hero_card(
            HeroCard(
                title="Welcome Card",
                text=f"Updated count - {data['count']}",
                buttons=[
                    CardAction(
                        type=ActionTypes.message_back,
                        title="Update Card",
                        value=data,
                        text="UpdateCardAction",
                    ),
                    CardAction(
                        type=ActionTypes.message_back,
                        title="Message all members",
                        text="MessageAllMembers",
                    ),
                    CardAction(
                        type=ActionTypes.message_back,
                        title="Delete card",
                        text="Delete",
                    ),
                ],
            )
        )

        updated_activity = MessageFactory.attachment(card)
        updated_activity.id = turn_context.activity.reply_to_id
        await turn_context.update_activity(updated_activity)

    async def _message_all_members(self, turn_context: TurnContext):
        team_members = await TeamsInfo.get_members(turn_context)

        for member in team_members:
            conversation_reference = TurnContext.get_conversation_reference(
                turn_context.activity
            )

            conversation_parameters = ConversationParameters(
                is_group=False,
                bot=turn_context.activity.recipient,
                members=[member],
                tenant_id=turn_context.activity.conversation.tenant_id,
            )

            async def get_ref(tc1):
                conversation_reference_inner = TurnContext.get_conversation_reference(
                    tc1.activity
                )
                return await tc1.adapter.continue_conversation(
                    conversation_reference_inner, send_message, self._app_id
                )

            async def send_message(tc2: TurnContext):
                return await tc2.send_activity(
                    f"Hello {member.name}. I'm a Teams conversation bot."
                )  # pylint: disable=cell-var-from-loop

            await turn_context.adapter.create_conversation(
                conversation_reference, get_ref, conversation_parameters
            )

        await turn_context.send_activity(
            MessageFactory.text("All messages have been sent")
        )

    async def _delete_activity(self, turn_context: TurnContext):
        activity = MessageFactory.text("This message will be deleted in 5 seconds")
        activity.reply_to_id = turn_context.activity.id
        activity_id = await turn_context.send_activity(activity)
        time.sleep(5)
        await turn_context.delete_activity(activity_id.id)

    async def _send_file_card(
        self, turn_context: TurnContext, filename: str, file_size: int
    ):
        consent_context = {"filename": filename}

        file_card = FileConsentCard(
            description="This is the file I want to send you",
            size_in_bytes=file_size,
            accept_context=consent_context,
            decline_context=consent_context,
        )

        as_attachment = Attachment(
            content=file_card.serialize(),
            content_type=ContentType.FILE_CONSENT_CARD,
            name=filename,
        )

        reply_activity = self._create_reply(turn_context.activity)
        reply_activity.attachments = [as_attachment]
        await turn_context.send_activity(reply_activity)

    async def _file_upload_complete(
        self,
        turn_context: TurnContext,
        file_consent_card_response: FileConsentCardResponse,
    ):
        
        """
        The file was uploaded, so display a FileInfoCard so the user can view the
        file in Teams.
        """

        name = file_consent_card_response.upload_info.name

        download_card = FileInfoCard(
            unique_id=file_consent_card_response.upload_info.unique_id,
            file_type=file_consent_card_response.upload_info.file_type,
        )

        as_attachment = Attachment(
            content=download_card.serialize(),
            content_type=ContentType.FILE_INFO_CARD,
            name=name,
            content_url=file_consent_card_response.upload_info.content_url,
        )

        reply = self._create_reply(
            turn_context.activity,
            f"<b>File uploaded.</b> Your file <b>{name}</b> is ready to download",
            "xml",
        )
        reply.attachments = [as_attachment]

        await turn_context.send_activity(reply)

    async def _file_upload_failed(self, turn_context: TurnContext, error: str):
        reply = self._create_reply(
            turn_context.activity,
            f"<b>File upload failed.</b> Error: <pre>{error}</pre>",
            "xml",
        )
        await turn_context.send_activity(reply)

    def _create_reply(self, activity, text=None, text_format=None):
        return Activity(
            type=ActivityTypes.message,
            timestamp=datetime.utcnow(),
            from_property=ChannelAccount(
                id=activity.recipient.id, name=activity.recipient.name
            ),
            recipient=ChannelAccount(
                id=activity.from_property.id, name=activity.from_property.name
            ),
            reply_to_id=activity.id,
            service_url=activity.service_url,
            channel_id=activity.channel_id,
            conversation=ConversationAccount(
                is_group=activity.conversation.is_group,
                id=activity.conversation.id,
                name=activity.conversation.name,
            ),
            text=text or "",
            text_format=text_format or None,
            locale=activity.locale,
        )

    async def _send_proactive_non_threaded_message(self, turn_context: TurnContext):
        conversation_reference = TurnContext.get_conversation_reference(
            turn_context.activity
        )

        conversation_parameters = ConversationParameters(
            is_group=False,
            bot=turn_context.activity.recipient,
            members=[turn_context.activity.from_property],
            tenant_id=turn_context.activity.conversation.tenant_id,
        )
        proactive_message = MessageFactory.text("This is a proactive message")
        proactive_message.label = turn_context.activity.id

        async def get_ref(tc1):
            conversation_reference_inner = TurnContext.get_conversation_reference(
                tc1.activity
            )
            return await tc1.adapter.continue_conversation(
                conversation_reference_inner, send_message, self._app_id
            )

        async def send_message(tc2: TurnContext):
            return await tc2.send_activity(proactive_message)

        await turn_context.adapter.create_conversation(
            conversation_reference, get_ref, conversation_parameters
        )

    async def _send_proactive_threaded_message(self, turn_context: TurnContext):
        activity = MessageFactory.text("I will send two messages to this thread")
        result = await turn_context.send_activity(activity)

        team_id = TeamsInfo.get_team_id(turn_context)
        for i in range(2):
            proactive_activity = MessageFactory.text(
                f"This is message {i+1}/2 that will be sent."
            )
            TurnContext.apply_conversation_reference(
                proactive_activity, TurnContext.get_conversation_reference(activity)
            )
            await turn_context.send_activity(proactive_activity)
