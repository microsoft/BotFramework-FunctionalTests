# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from botbuilder.schema import Activity
from skills.skill_definition import SkillDefinition

SKILL_ACTION_TEAMS_TASK_MODULE = "TeamsTaskModule"
SKILL_ACTION_TEAMS_CARD_ACTION = "TeamsCardAction"
SKILL_ACTION_TEAMS_CONVERSATION = "TeamsConversation"


class TeamsSkill(SkillDefinition):
    def get_actions(self):
        return [
            SKILL_ACTION_TEAMS_TASK_MODULE,
            SKILL_ACTION_TEAMS_CARD_ACTION,
            SKILL_ACTION_TEAMS_CONVERSATION,
        ]

    def create_begin_activity(self, action_id: str):
        activity = Activity.create_event_activity()

        if action_id == SKILL_ACTION_TEAMS_TASK_MODULE:
            activity.name = SKILL_ACTION_TEAMS_TASK_MODULE

        elif action_id == SKILL_ACTION_TEAMS_CARD_ACTION:
            activity.name = SKILL_ACTION_TEAMS_CARD_ACTION

        elif action_id == SKILL_ACTION_TEAMS_CONVERSATION:
            activity.name = SKILL_ACTION_TEAMS_CONVERSATION

        else:
            raise Exception(
                f'[TeamsSkill]: Unable to create begin activity for "{ action_id }".'
            )

        return activity
