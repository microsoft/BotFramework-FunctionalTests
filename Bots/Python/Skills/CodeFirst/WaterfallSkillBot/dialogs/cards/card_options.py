# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from enum import Enum


class CardOptions(str, Enum):
    adaptive_card_bot_action = "AdaptiveCardBotAction"
    adaptive_card_task_module = "AdaptiveCardTaskModule"
    adaptive_card_sumbit_action = "AdaptiveCardSumbitAction"
    hero = "Hero"
    thumbnail = "Thumbnail"
    receipt = "Receipt"
    signin = "Signin"
    carousel = "Carousel"
    list = "List"
    o365 = "O365"
    teams_file_consent = "TeamsFileConsent"
    animation = "Animation"
    audio = "Audio"
    video = "Video"
    end = "End"
