# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from botbuilder.core import (
    InvokeResponse,
    TurnContext,
)

from typing import Union

from botbuilder.integration.aiohttp import BotFrameworkHttpClient
from botbuilder.core.skills import (
    ConversationIdFactoryBase,
    SkillConversationIdFactoryOptions,
    SkillConversationReference,
    BotFrameworkSkill,
)
from botbuilder.schema import Activity, ActivityTypes, ConversationReference
from botframework.connector.auth import SimpleCredentialProvider

class SimpleConversationIdFactory(ConversationIdFactoryBase):
    def __init__(self):
        self.conversation_refs = {}

    async def create_skill_conversation_id(
        self,
        options_or_conversation_reference: Union[
            SkillConversationIdFactoryOptions, ConversationReference
        ],
    ) -> str:
        key = (
            options_or_conversation_reference.activity.conversation.id
            + options_or_conversation_reference.activity.service_url
        )
        if key not in self.conversation_refs:
            self.conversation_refs[key] = SkillConversationReference(
                conversation_reference=TurnContext.get_conversation_reference(
                    options_or_conversation_reference.activity
                ),
                oauth_scope=options_or_conversation_reference.from_bot_oauth_scope,
            )
        return key

    async def get_conversation_reference(
        self, skill_conversation_id: str
    ) -> Union[SkillConversationReference, ConversationReference]:
        return self.conversation_refs[skill_conversation_id]

    async def delete_conversation_reference(self, skill_conversation_id: str):
        raise NotImplementedError()

class SkillHttpClient(BotFrameworkHttpClient):
    def __init__(
        self,
        credential_provider: SimpleCredentialProvider,
        skill_conversation_id_factory: SimpleConversationIdFactory,
    ):
        if not skill_conversation_id_factory:
            raise TypeError("skill_conversation_id_factory can't be None")

        super().__init__(credential_provider)

        self._skill_conversation_id_factory = skill_conversation_id_factory

    async def post_activity(
        self,
        from_bot_id: str,
        to_skill: BotFrameworkSkill,
        service_url: str,
        activity: Activity,
    ) -> InvokeResponse:

        skill_conversation_id = await self._skill_conversation_id_factory.create_skill_conversation_id(
            TurnContext.get_conversation_reference(activity)
        )
        return await super().post_activity(
            from_bot_id,
            to_skill.app_id,
            to_skill.skill_endpoint,
            service_url,
            skill_conversation_id,
            activity,
        )
