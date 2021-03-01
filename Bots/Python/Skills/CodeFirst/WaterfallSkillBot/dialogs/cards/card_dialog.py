# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import json

from botbuilder.core import MessageFactory
from botbuilder.dialogs import (
    ComponentDialog,
    DialogTurnResult,
    DialogTurnStatus,
    WaterfallDialog,
    WaterfallStepContext,
    Choice,
    ListStyle,
)
from botbuilder.dialogs.prompts import (
    ChoicePrompt,
    PromptOptions,
    PromptValidatorContext,
)
from botbuilder.schema import (
    Activity,
    ActivityTypes,
    EndOfConversationCodes,
    InputHints,
)
from config import DefaultConfig
from .card_options import CardOptions
from .channel_supported_cards import ChannelSupportedCards
from .card_sample_helper import CardSampleHelper


CORGI_ON_CAROUSEL_VIDEO = "https://www.youtube.com/watch?v=LvqzubPZjHE"
MIND_BLOWN_GIF = (
    "https://media3.giphy.com/media/xT0xeJpnrWC4XWblEk/giphy.gif?"
    "cid=ecf05e47mye7k75sup6tcmadoom8p1q8u03a7g2p3f76upp9&rid=giphy.gif"
)
MUSIC_API = "api/music"
TEAMS_LOGO_FILE_NAME = "teams-logo.png"


class CardDialog(ComponentDialog):
    def __init__(self, configuration: DefaultConfig):
        super().__init__(CardDialog.__name__)
        self.configuration = configuration

        self.add_dialog(ChoicePrompt(ChoicePrompt.__name__, self.card_prompt_validator))
        self.add_dialog(
            WaterfallDialog(
                WaterfallDialog.__name__,
                [self.select_card_step, self.display_card_step],
            )
        )

        self.initial_dialog_id = WaterfallDialog.__name__

    async def select_card_step(self, step_context: WaterfallStepContext):
        # Create the PromptOptions from the skill configuration which contain the list of configured skills.
        message_text = "What card do you want?"
        reprompt_message_text = "This message will be created in the validation code"

        options = PromptOptions(
            prompt=MessageFactory.text(
                message_text, message_text, InputHints.expecting_input
            ),
            retry_prompt=MessageFactory.text(
                reprompt_message_text, reprompt_message_text, InputHints.expecting_input
            ),
            choices=[Choice(card.value) for card in CardOptions],
            style=ListStyle.list_style,
        )

        return await step_context.prompt(ChoicePrompt.__name__, options)

    async def display_card_step(self, step_context: WaterfallStepContext):
        card_type = CardOptions(step_context.result.value)

        if ChannelSupportedCards.is_card_supported(
            step_context.context.activity.channel_id, card_type
        ):
            if card_type == CardOptions.adaptive_card_bot_action:
                await step_context.context.send_activity(
                    MessageFactory.attachment(
                        CardSampleHelper.create_adaptive_card_bot_action()
                    )
                )

            elif card_type == CardOptions.adaptive_card_task_module:
                await step_context.context.send_activity(
                    MessageFactory.attachment(
                        CardSampleHelper.create_adaptive_card_task_module()
                    )
                )

            elif card_type == CardOptions.adaptive_card_sumbit_action:
                await step_context.context.send_activity(
                    MessageFactory.attachment(
                        CardSampleHelper.create_adaptive_card_submit()
                    )
                )

            elif card_type == CardOptions.hero:
                await step_context.context.send_activity(
                    MessageFactory.attachment(CardSampleHelper.create_hero_card())
                )

            elif card_type == CardOptions.thumbnail:
                await step_context.context.send_activity(
                    MessageFactory.attachment(CardSampleHelper.create_thumbnail_card())
                )

            elif card_type == CardOptions.receipt:
                await step_context.context.send_activity(
                    MessageFactory.attachment(CardSampleHelper.create_receipt_card())
                )

            elif card_type == CardOptions.signin:
                await step_context.context.send_activity(
                    MessageFactory.attachment(CardSampleHelper.create_signin_card())
                )

            elif card_type == CardOptions.carousel:
                #  NOTE if cards are NOT the same height in a carousel,
                #  Teams will instead display as AttachmentLayoutTypes.List
                await step_context.context.send_activity(
                    MessageFactory.carousel(
                        [
                            CardSampleHelper.create_hero_card(),
                            CardSampleHelper.create_hero_card(),
                            CardSampleHelper.create_hero_card(),
                        ]
                    )
                )

            elif card_type == CardOptions.list:
                await step_context.context.send_activity(
                    MessageFactory.list(
                        [
                            CardSampleHelper.create_hero_card(),
                            CardSampleHelper.create_hero_card(),
                            CardSampleHelper.create_hero_card(),
                        ]
                    )
                )

            elif card_type == CardOptions.o365:
                await step_context.context.send_activity(
                    MessageFactory.attachment(
                        CardSampleHelper.create_o365_connector_card()
                    )
                )

            elif card_type == CardOptions.teams_file_consent:
                await step_context.context.send_activity(
                    MessageFactory.attachment(
                        CardSampleHelper.create_teams_file_consent_card(
                            TEAMS_LOGO_FILE_NAME
                        )
                    )
                )

            elif card_type == CardOptions.animation:
                await step_context.context.send_activity(
                    MessageFactory.attachment(
                        CardSampleHelper.create_animation_card(MIND_BLOWN_GIF)
                    )
                )

            elif card_type == CardOptions.audio:
                await step_context.context.send_activity(
                    MessageFactory.attachment(
                        CardSampleHelper.create_audio_card(
                            f"{self.configuration.SERVER_URL}/{MUSIC_API}"
                        )
                    )
                )

            elif card_type == CardOptions.video:
                await step_context.context.send_activity(
                    MessageFactory.attachment(
                        CardSampleHelper.create_video_card(CORGI_ON_CAROUSEL_VIDEO)
                    )
                )

            elif card_type == CardOptions.end:
                # End the dialog so the host gets an EoC
                await step_context.context.send_activity(
                    Activity(
                        type=ActivityTypes.end_of_conversation,
                        code=EndOfConversationCodes.completed_successfully,
                    )
                )
                return DialogTurnResult(DialogTurnStatus.Complete)

        else:
            await step_context.context.send_activity(
                f"{card_type.value} cards are not supported in the {step_context.context.activity.channel_id} channel."
            )

        return await step_context.replace_dialog(
            self.initial_dialog_id, "What card would you want?"
        )

    @staticmethod
    async def card_prompt_validator(prompt_context: PromptValidatorContext) -> bool:
        if not prompt_context.recognized.succeeded:
            if prompt_context.context.activity.attachments:
                return True

            # Render the activity so we can assert in tests.
            # We may need to simplify the json if it gets too complicated to test.
            activity_json = json.dumps(
                prompt_context.context.activity.__dict__, indent=4, default=str
            ).replace("\n", "\r\n")
            prompt_context.options.retry_prompt.text = (
                f"Got {activity_json}\n\n{prompt_context.options.prompt.text}"
            )
            return False
        return True
