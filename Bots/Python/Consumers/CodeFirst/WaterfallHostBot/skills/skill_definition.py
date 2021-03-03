# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from botbuilder.core.skills import BotFrameworkSkill


class SkillDefinition(BotFrameworkSkill):
    def get_actions(self):
        raise NotImplementedError("[SkillDefinition]: Method not implemented")

    def create_begin_activity(self, action_id: str):
        raise NotImplementedError("[SkillDefinition]: Method not implemented")
