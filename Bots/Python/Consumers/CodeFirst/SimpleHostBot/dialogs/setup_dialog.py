# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from botbuilder.dialogs import (
    ComponentDialog,
    WaterfallDialog,
    WaterfallStepContext,
    DialogTurnResult,
)
from botbuilder.dialogs.prompts import (
    TextPrompt,
    ChoicePrompt,
    PromptOptions,
)
from botbuilder.dialogs.choices import Choice
from botbuilder.core import MessageFactory, ConversationState
from botbuilder.schema import DeliveryModes

from bots.host_bot import ACTIVE_SKILL_PROPERTY_NAME, DELIVERY_MODE_PROPERTY_NAME
from config import SkillConfiguration


class SetupDialog(ComponentDialog):
    def __init__(
        self, conversation_state: ConversationState, skills_config: SkillConfiguration
    ):
        super(SetupDialog, self).__init__(SetupDialog.__name__)

        self._delivery_mode_property = conversation_state.create_property(
            DELIVERY_MODE_PROPERTY_NAME
        )
        self._active_skill_property = conversation_state.create_property(
            ACTIVE_SKILL_PROPERTY_NAME
        )

        self._skills_config = skills_config

        # Define the setup dialog and its related components.
        # Add ChoicePrompt to render available skills.
        self.add_dialog(ChoicePrompt(self.select_delivery_mode_step.__name__))
        self.add_dialog(ChoicePrompt(self.select_skill_step.__name__))
        self.add_dialog(TextPrompt(self.final_step.__name__))
        # Add main waterfall dialog for this bot.
        self.add_dialog(
            WaterfallDialog(
                WaterfallDialog.__name__,
                [
                    self.select_delivery_mode_step,
                    self.select_skill_step,
                    self.final_step,
                ],
            )
        )
        self.initial_dialog_id = WaterfallDialog.__name__

    # Render a prompt to select the delivery mode to use.
    async def select_delivery_mode_step(
        self, step_context: WaterfallStepContext
    ) -> DialogTurnResult:
        return await step_context.prompt(
            self.select_delivery_mode_step.__name__,
            # Create the PromptOptions with the delivery modes supported.
            PromptOptions(
                prompt=MessageFactory.text("What delivery mode would you like to use?"),
                retry_prompt=MessageFactory.text(
                    "That was not a valid choice, please select a valid delivery mode."
                ),
                choices=[
                    Choice("normal"),
                    Choice("expectReplies"),
                ],
            ),
        )

    # Render a prompt to select the skill to call.
    async def select_skill_step(
        self, step_context: WaterfallStepContext
    ) -> DialogTurnResult:
        # Set delivery mode.
        await self._delivery_mode_property.set(
            step_context.context, step_context.result.value
        )
        return await step_context.prompt(
            self.select_skill_step.__name__,
            # Create the PromptOptions from the skill configuration which contains the list of configured skills.
            PromptOptions(
                prompt=MessageFactory.text("What skill would you like to call?"),
                retry_prompt=MessageFactory.text(
                    "That was not a valid choice, please select a valid skill."
                ),
                choices=[
                    Choice(value=skill.id)
                    for _, skill in self._skills_config.SKILLS.items()
                ],
            ),
        )

    # The SetupDialog has ended, we go back to the HostBot to connect with the selected skill.
    async def final_step(self, step_context: WaterfallStepContext) -> DialogTurnResult:
        # Set active skill.
        selected_skill = self._skills_config.SKILLS.get(step_context.result.value)
        await self._active_skill_property.set(step_context.context, selected_skill)

        await step_context.context.send_activity(
            MessageFactory.text("Type anything to send to the skill.")
        )

        return await step_context.end_dialog()
