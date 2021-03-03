# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from botbuilder.schema import Activity
from skills.skill_definition import SkillDefinition

SKILL_ACTION_MESSAGE = "Message"


class EchoSkill(SkillDefinition):
    def get_actions(self):
        return [SKILL_ACTION_MESSAGE]

    def create_begin_activity(self, action_id: str):
        activity = Activity.create_message_activity()

        if action_id == SKILL_ACTION_MESSAGE:
            activity.name = SKILL_ACTION_MESSAGE
            activity.text = "Begin the Echo Skill"

        else:
            raise Exception(f'Unable to create begin activity for "${action_id}"')

        return activity
