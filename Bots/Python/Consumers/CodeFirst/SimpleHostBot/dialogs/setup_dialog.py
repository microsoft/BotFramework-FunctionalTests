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
from botbuilder.schema import InputHints

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
        # Create the PromptOptions with the delivery modes supported.
        message = "What delivery mode would you like to use?"
        reprompt_message = (
            "That was not a valid choice, please select a valid delivery mode."
        )
        options = PromptOptions(
            prompt=MessageFactory.text(message, message, InputHints.expecting_input),
            retry_prompt=MessageFactory.text(
                reprompt_message, reprompt_message, InputHints.expecting_input
            ),
            choices=[Choice("normal"), Choice("expectReplies")],
        )
        return await step_context.prompt(
            self.select_delivery_mode_step.__name__, options
        )

    # Render a prompt to select the skill to call.
    async def select_skill_step(
        self, step_context: WaterfallStepContext
    ) -> DialogTurnResult:
        # Set delivery mode.
        await self._delivery_mode_property.set(
            step_context.context, step_context.result.value
        )

        # Create the PromptOptions from the skill configuration which contains the list of configured skills.
        message = "What skill would you like to call?"
        reprompt_message = "That was not a valid choice, please select a valid skill."
        options = PromptOptions(
            prompt=MessageFactory.text(message, message, InputHints.expecting_input),
            retry_prompt=MessageFactory.text(reprompt_message, reprompt_message),
            choices=[
                Choice(value=skill.id)
                for _, skill in sorted(self._skills_config.SKILLS.items())
            ],
        )

        return await step_context.prompt(self.select_skill_step.__name__, options)

    # The SetupDialog has ended, we go back to the HostBot to connect with the selected skill.
    async def final_step(self, step_context: WaterfallStepContext) -> DialogTurnResult:
        # Set active skill.
        for i in self._skills_config.SKILLS.keys():
            if i.lower() == step_context.result.value.lower():
                selected_skill = self._skills_config.SKILLS.get(i)

        await self._active_skill_property.set(step_context.context, selected_skill)

        await step_context.context.send_activity(
            MessageFactory.text("Type anything to send to the skill.")
        )

        return await step_context.end_dialog()