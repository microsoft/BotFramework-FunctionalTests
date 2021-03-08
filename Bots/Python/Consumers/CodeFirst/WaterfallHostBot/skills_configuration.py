# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import os
from typing import Dict
from botbuilder.core.skills import BotFrameworkSkill
from botbuilder.dialogs import ObjectPath
from dotenv import load_dotenv

from skills.skill_definition import SkillDefinition
from skills.waterfall_skill import WaterfallSkill
from skills.echo_skill import EchoSkill
from skills.teams_skill import TeamsSkill

load_dotenv()


class DefaultConfig:
    """
    Bot Default Configuration
    """

    PORT = 37020
    APP_ID = os.getenv("MicrosoftAppId")
    APP_PASSWORD = os.getenv("MicrosoftAppPassword")
    SSO_CONNECTION_NAME = os.getenv("SsoConnectionName")


class SkillsConfiguration:
    """
    Bot Skills Configuration
    """

    SKILL_HOST_ENDPOINT = os.getenv("SkillHostEndpoint")
    SKILLS: Dict[str, SkillDefinition]

    def __init__(self):
        skills_data = dict()
        skill_variable = [x for x in os.environ if x.lower().startswith("skill_")]

        for val in skill_variable:
            names = val.split("_")
            bot_id = names[1]
            attr = names[2]

            if bot_id not in skills_data:
                skills_data[bot_id] = self.create_skill_definition(
                    BotFrameworkSkill(id=bot_id)
                )

            if attr.lower() == "appid":
                skills_data[bot_id].app_id = os.getenv(val)
            elif attr.lower() == "endpoint":
                skills_data[bot_id].skill_endpoint = os.getenv(val)
            else:
                raise ValueError(
                    f"[SkillsConfiguration]: Invalid environment variable declaration {attr}"
                )

        SkillsConfiguration.SKILLS = skills_data

    # Note: we hard code this for now, we should dynamically create instances based on the manifests.
    # For now, this code creates a strong typed version of the SkillDefinition and copies the info from
    # settings to it.
    @staticmethod
    def create_skill_definition(skill: BotFrameworkSkill):
        if skill.id.lower().startswith("echoskillbot"):
            skill_definition = ObjectPath.assign(EchoSkill(), skill)

        elif skill.id.lower().startswith("waterfallskillbot"):
            skill_definition = ObjectPath.assign(WaterfallSkill(), skill)

        elif skill.id.lower().startswith("teamsskillbot"):
            skill_definition = ObjectPath.assign(TeamsSkill(), skill)

        else:
            raise Exception(f"Unable to find definition class for {skill.id}.")

        return skill_definition
