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
        "MicrosoftAppId", ""
    )
    APP_PASSWORD = os.environ.get(
        "MicrosoftAppPassword", ""
    )
    SKILL_HOST_ENDPOINT = os.getenv("SKILL_HOST_ENDPOINT")
    SKILLS = [
        {
            "id": "EchoSkillBot",
            "app_id": os.getenv("SKILL_BOT_APP_ID"),
            "skill_endpoint": os.getenv("SKILL_BOT_ENDPOINT"),
        },
    ]

    # Callers to only those specified, '*' allows any caller.
    # Example: os.environ.get("AllowedCallers", ["54d3bb6a-3b6d-4ccd-bbfd-cad5c72fb53a"])
    ALLOWED_CALLERS = os.environ.get("AllowedCallers", ["*"])


class SkillConfiguration:
    SKILL_HOST_ENDPOINT = DefaultConfig.SKILL_HOST_ENDPOINT
    SKILLS: Dict[str, BotFrameworkSkill] = {
        skill["id"]: BotFrameworkSkill(**skill) for skill in DefaultConfig.SKILLS
    }
