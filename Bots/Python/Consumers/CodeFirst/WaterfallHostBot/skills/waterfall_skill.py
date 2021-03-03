# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from botbuilder.schema import Activity
from skills.skill_definition import SkillDefinition

SKILL_ACTION_CARDS = "Cards"
SKILL_ACTION_PROACTIVE = "Proactive"
SKILL_ACTION_AUTH = "Auth"
SKILL_ACTION_MESSAGE_WITH_ATTACHMENT = "MessageWithAttachment"
SKILL_ACTION_SSO = "Sso"
SKILL_ACTION_FILE_UPLOAD = "FileUpload"
SKILL_ACTION_CALL_ECHO_SKILL = "Echo"


class WaterfallSkill(SkillDefinition):
    def get_actions(self):
        return [
            SKILL_ACTION_CARDS,
            SKILL_ACTION_PROACTIVE,
            SKILL_ACTION_AUTH,
            SKILL_ACTION_MESSAGE_WITH_ATTACHMENT,
            SKILL_ACTION_SSO,
            SKILL_ACTION_FILE_UPLOAD,
            SKILL_ACTION_CALL_ECHO_SKILL,
        ]

    def create_begin_activity(self, action_id: str):
        activity = Activity.create_event_activity()

        if action_id == SKILL_ACTION_CARDS:
            activity.name = SKILL_ACTION_CARDS

        elif action_id == SKILL_ACTION_PROACTIVE:
            activity.name = SKILL_ACTION_PROACTIVE

        elif action_id == SKILL_ACTION_AUTH:
            activity.name = SKILL_ACTION_AUTH

        elif action_id == SKILL_ACTION_MESSAGE_WITH_ATTACHMENT:
            activity.name = SKILL_ACTION_MESSAGE_WITH_ATTACHMENT

        elif action_id == SKILL_ACTION_SSO:
            activity.name = SKILL_ACTION_SSO

        elif action_id == SKILL_ACTION_FILE_UPLOAD:
            activity.name = SKILL_ACTION_FILE_UPLOAD

        elif action_id == SKILL_ACTION_CALL_ECHO_SKILL:
            activity.name = SKILL_ACTION_CALL_ECHO_SKILL

        else:
            raise Exception(
                f'[WaterfallSkill]: Unable to create begin activity for "{ action_id }".'
            )

        return activity
