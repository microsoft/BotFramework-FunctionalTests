#!/usr/bin/env python3
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import os
from typing import Dict
from botbuilder.core.skills import BotFrameworkSkill
from dotenv import load_dotenv

""" Bot Configuration """


class DefaultConfig:
    """ Bot Configuration """

    load_dotenv()

    PORT = 3978
    APP_ID = os.environ.get(
        "MicrosoftAppId", "TODO: Add here the App ID for the host bot"
    )
    APP_PASSWORD = os.environ.get(
        "MicrosoftAppPassword", "TODO: Add here the App Password for the host bot"
    )
    SKILL_HOST_ENDPOINT = os.getenv("SKILL_HOST_ENDPOINT")
    SKILLS = [
        {
            "id": "EchoSkillBot",
            "app_id": os.getenv("SKILL_BOT_APP_ID"),
            "skill_endpoint": os.getenv("SKILL_BOT_ENDPOINT"),
        },
    ]


class SkillConfiguration:
    SKILL_HOST_ENDPOINT = DefaultConfig.SKILL_HOST_ENDPOINT
    SKILLS: Dict[str, BotFrameworkSkill] = {
        skill["id"]: BotFrameworkSkill(**skill) for skill in DefaultConfig.SKILLS
    }
