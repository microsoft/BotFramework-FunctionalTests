# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import copy
import json

from botbuilder.dialogs import (
    ComponentDialog,
    DialogContext,
    WaterfallDialog,
    WaterfallStepContext,
    DialogTurnResult,
    ListStyle,
)
from botbuilder.dialogs.choices import Choice, FoundChoice
from botbuilder.dialogs.prompts import (
    PromptOptions,
    ChoicePrompt,
    PromptValidatorContext,
)
from botbuilder.dialogs.skills import (
    SkillDialogOptions,
    SkillDialog,
    BeginSkillDialogOptions,
)
from botbuilder.core import ConversationState, MessageFactory, TurnContext
from botbuilder.core.skills import ConversationIdFactoryBase
from botbuilder.schema import Activity, ActivityTypes, InputHints, DeliveryModes
from botbuilder.integration.aiohttp.skills import SkillHttpClient

from skills_configuration import SkillsConfiguration, DefaultConfig

from dialogs.tangent_dialog import TangentDialog
from dialogs.sso.sso_dialog import SsoDialog


ACTIVE_SKILL_PROPERTY_NAME = "MainDialog.ActiveSkillProperty"
DELIVERY_MODE_NAME = "MainDialog.DeliveryMode"
SELECTED_SKILL_KEY_NAME = "MainDialog.SelectedSkillKey"
JUST_FORWARD_THE_ACTIVITY = "JustForwardTurnContext.Activity"

DELIVERY_MODE_PROMPT = "DeliveryModePrompt"
SKILL_TYPE_PROMPT = "SkillTypePrompt"
SKILL_PROMPT = "SkillPrompt"
SKILL_ACTION_PROMPT = "SkillActionPrompt"


class MainDialog(ComponentDialog):
    def __init__(
        self,
        conversation_state: ConversationState,
        conversation_id_factory: ConversationIdFactoryBase,
        skill_client: SkillHttpClient,
        skills_config: SkillsConfiguration,
        configuration: DefaultConfig,
    ):
        super().__init__(MainDialog.__name__)

        bot_id = configuration.APP_ID

        self._skills_config = skills_config
        if not self._skills_config:
            raise TypeError(
                "[MainDialog]: Missing parameter. skills_config is required"
            )

        if not skill_client:
            raise TypeError("[MainDialog]: Missing parameter. skill_client is required")

        if not conversation_state:
            raise TypeError(
                "[MainDialog]: Missing parameter. conversation_state is required"
            )

        if not conversation_id_factory:
            raise TypeError(
                "[MainDialog]: Missing parameter. conversation_id_factory is required"
            )

        # Use helper method to add SkillDialog instances for the configured skills.
        self._add_skill_dialogs(
            conversation_state,
            conversation_id_factory,
            skill_client,
            skills_config,
            bot_id,
        )

        # Create state property to track the active skill.
        self.active_skill_property = conversation_state.create_property(
            ACTIVE_SKILL_PROPERTY_NAME
        )

        # Register the tangent dialog for testing tangents and resume
        self.add_dialog(TangentDialog())

        # Add ChoicePrompt to render available delivery modes.
        self.add_dialog(ChoicePrompt(DELIVERY_MODE_PROMPT))

        # Add ChoicePrompt to render available types of skill.
        self.add_dialog(ChoicePrompt(SKILL_TYPE_PROMPT))

        # Add ChoicePrompt to render available skills.
        self.add_dialog(ChoicePrompt(SKILL_PROMPT))

        # Add ChoicePrompt to render skill actions.
        self.add_dialog(
            ChoicePrompt(SKILL_ACTION_PROMPT, self._skill_action_prompt_validator)
        )

        # Add dialog to prepare SSO on the host and test the SSO skill
        # The waterfall skillDialog created in AddSkillDialogs contains the SSO skill action.
        waterfall_skills = [
            skill_dialog
            for skill_dialog in self._dialogs._dialogs.values()
            if skill_dialog.id.startswith("WATERFALLSKILL")
        ]

        for waterfall_skill in waterfall_skills:
            self.add_dialog(SsoDialog(configuration, waterfall_skill))

        self.add_dialog(
            WaterfallDialog(
                WaterfallDialog.__name__,
                [
                    self._select_delivery_mode_step,
                    self._select_skill_type_step,
                    self._select_skill_step,
                    self._select_skill_action_step,
                    self._call_skill_action_step,
                    self._final_step,
                ],
            )
        )

        self.initial_dialog_id = WaterfallDialog.__name__

    async def on_continue_dialog(self, inner_dc: DialogContext):
        """
        This override is used to test the "abort" command to interrupt skills from the parent and
        also to test the "tangent" command to start a tangent and resume a skill.
        """

        # This is an example on how to cancel a SkillDialog that is currently in progress from the parent bot.
        active_skill = await self.active_skill_property.get(inner_dc.context)
        activity = inner_dc.context.activity

        if (
            active_skill
            and activity.type == ActivityTypes.message
            and "abort" in activity.text.lower()
        ):
            # Cancel all dialogs when the user says abort.
            # The SkillDialog automatically sends an EndOfConversation message to the skill to let the
            # skill know that it needs to end its current dialogs, too.
            await inner_dc.cancel_all_dialogs()
            return await inner_dc.replace_dialog(
                self.initial_dialog_id,
                "Canceled! \n\n What delivery mode would you like to use?",
            )

        # Sample to test a tangent when in the middle of a skill conversation.
        if (
            active_skill
            and activity.type == ActivityTypes.message
            and activity.text
            and activity.text.lower() == "tangent"
        ):
            # Start tangent.
            return await inner_dc.begin_dialog(TangentDialog.__name__)

        return await super().on_continue_dialog(inner_dc)

    async def _select_delivery_mode_step(
        self, step_context: WaterfallStepContext
    ) -> DialogTurnResult:
        """
        Render a prompt to select the delivery mode to use.
        """

        # Create the PromptOptions with the delivery modes supported.
        message_text = (
            str(step_context.options)
            if step_context.options
            else "What delivery mode would you like to use?"
        )
        reprompt_message_text = (
            "That was not a valid choice, please select a valid delivery mode."
        )

        options = PromptOptions(
            prompt=MessageFactory.text(
                message_text, message_text, InputHints.expecting_input
            ),
            retry_prompt=MessageFactory.text(
                reprompt_message_text, reprompt_message_text, InputHints.expecting_input
            ),
            style=ListStyle.suggested_action,
            choices=[
                Choice(DeliveryModes.normal.value),
                Choice(DeliveryModes.expect_replies.value),
            ],
        )

        # Prompt the user to select a delivery mode.
        return await step_context.prompt(DELIVERY_MODE_PROMPT, options)

    async def _select_skill_type_step(self, step_context: WaterfallStepContext):
        """
        Render a prompt to select the type of skill to use.
        """

        # Remember the delivery mode selected by the user.
        step_context.values[DELIVERY_MODE_NAME] = step_context.result.value

        # Create the PromptOptions with the types of supported skills.
        message_text = "What type of skill would you like to use?"
        reprompt_message_text = (
            "That was not a valid choice, please select a valid skill type."
        )

        options = PromptOptions(
            prompt=MessageFactory.text(
                message_text, message_text, InputHints.expecting_input
            ),
            retry_prompt=MessageFactory.text(
                reprompt_message_text, reprompt_message_text, InputHints.expecting_input
            ),
            style=ListStyle.suggested_action,
            choices=[Choice("EchoSkill"), Choice("WaterfallSkill")],
        )

        # Prompt the user to select a type of skill.
        return await step_context.prompt(SKILL_TYPE_PROMPT, options)

    async def _select_skill_step(
        self, step_context: WaterfallStepContext
    ) -> DialogTurnResult:
        """
        Render a prompt to select the skill to call.
        """

        skill_type = step_context.result.value

        # Create the PromptOptions from the skill configuration which contain the list of configured skills.
        message_text = "What skill would you like to call?"
        reprompt_message_text = (
            "That was not a valid choice, please select a valid skill."
        )

        options = PromptOptions(
            prompt=MessageFactory.text(
                message_text, message_text, InputHints.expecting_input
            ),
            retry_prompt=MessageFactory.text(
                reprompt_message_text, reprompt_message_text, InputHints.expecting_input
            ),
            style=ListStyle.suggested_action,
            choices=[
                Choice(val.id)
                for key, val in self._skills_config.SKILLS.items()
                if key.lower().startswith(skill_type.lower())
            ],
        )

        # Prompt the user to select a skill.
        return await step_context.prompt(SKILL_PROMPT, options)

    async def _select_skill_action_step(self, step_context: WaterfallStepContext):
        """
        Render a prompt to select the begin action for the skill.
        """

        # Get the skill info based on the selected skill.
        selected_skill_id = step_context.result.value
        delivery_mode = str(step_context.values[DELIVERY_MODE_NAME])
        v3_bots = ["EchoSkillBotDotNetV3", "EchoSkillBotJSV3"]

        # Exclude v3 bots from ExpectReplies
        if (
            delivery_mode == DeliveryModes.expect_replies
            and selected_skill_id in v3_bots
        ):
            await step_context.context.send_activity(
                MessageFactory.text(
                    "V3 Bots do not support 'expectReplies' delivery mode."
                )
            )

            # Restart setup dialog
            return await step_context.replace_dialog(self.initial_dialog_id)

        selected_skill = next(
            val
            for key, val in self._skills_config.SKILLS.items()
            if val.id == selected_skill_id
        )

        # Remember the skill selected by the user.
        step_context.values[SELECTED_SKILL_KEY_NAME] = selected_skill

        # Create the PromptOptions with the actions supported by the selected skill.
        message_text = (
            f"Select an action # to send to **{selected_skill.id}**.\n\nOr just type in "
            f"a message and it will be forwarded to the skill as a message activity."
        )

        options = PromptOptions(
            prompt=MessageFactory.text(
                message_text, message_text, InputHints.expecting_input
            ),
            choices=[Choice(action) for action in selected_skill.get_actions()],
        )

        # Prompt the user to select a skill action.
        return await step_context.prompt(SKILL_ACTION_PROMPT, options)

    async def _skill_action_prompt_validator(
        self, prompt_context: PromptValidatorContext
    ):
        """
        This validator defaults to Message if the user doesn't select an existing option.
        """

        if not prompt_context.recognized.succeeded:
            # Assume the user wants to send a message if an item in the list is not selected.
            prompt_context.recognized.value = FoundChoice(
                value=JUST_FORWARD_THE_ACTIVITY,
                index=0,
                score=0,
            )

        return True

    async def _call_skill_action_step(self, step_context: WaterfallStepContext):
        """
        Starts the SkillDialog based on the user's selections.
        """

        selected_skill = step_context.values[SELECTED_SKILL_KEY_NAME]
        skill_activity = self._create_begin_activity(
            step_context.context, selected_skill.id, step_context.result.value
        )

        # Create the BeginSkillDialogOptions and assign the activity to send.
        skill_dialog_args = BeginSkillDialogOptions(activity=skill_activity)
        delivery_mode = str(step_context.values[DELIVERY_MODE_NAME])

        if delivery_mode == DeliveryModes.expect_replies:
            skill_dialog_args.activity.delivery_mode = DeliveryModes.expect_replies

        # Save active skill in state.
        await self.active_skill_property.set(step_context.context, selected_skill)

        if skill_activity.name == "Sso":
            # Special case, we start the SSO dialog to prepare the host to call the skill.
            return await step_context.begin_dialog(
                SsoDialog.__name__ + selected_skill.id
            )

        # Start the skillDialog instance with the arguments.
        return await step_context.begin_dialog(selected_skill.id, skill_dialog_args)

    async def _final_step(self, step_context: WaterfallStepContext):
        """
        The SkillDialog has ended, render the results (if any) and restart MainDialog.
        """

        active_skill = await self.active_skill_property.get(step_context.context)

        # Check if the skill returned any results and display them.
        if step_context.result:
            message = f'Skill "{active_skill.id}" invocation complete.'
            message += f" Result: {json.dumps(step_context.result)}"
            await step_context.context.send_activity(
                MessageFactory.text(message, message, InputHints.ignoring_input)
            )

        # Clear the delivery mode selected by the user.
        step_context.values[DELIVERY_MODE_NAME] = None

        # Clear the skill selected by the user.
        step_context.values[SELECTED_SKILL_KEY_NAME] = None

        # Clear active skill in state.
        await self.active_skill_property.delete(step_context.context)

        # Restart the main dialog with a different message the second time around.
        return await step_context.replace_dialog(
            self.initial_dialog_id,
            f'Done with "{active_skill.id}". \n\n What delivery mode would you '
            f"like to use?",
        )

    def _add_skill_dialogs(
        self,
        conversation_state: ConversationState,
        conversation_id_factory: ConversationIdFactoryBase,
        skill_client: SkillHttpClient,
        skills_config: SkillsConfiguration,
        bot_id: str,
    ):
        """
        Helper method that creates and adds SkillDialog instances for the configured skills.
        """

        for skill_info in self._skills_config.SKILLS.values():
            # Create the dialog options.
            skill_dialog_options = SkillDialogOptions(
                bot_id=bot_id,
                conversation_id_factory=conversation_id_factory,
                skill_client=skill_client,
                skill_host_endpoint=skills_config.SKILL_HOST_ENDPOINT,
                conversation_state=conversation_state,
                skill=skill_info,
            )

            # Add a SkillDialog for the selected skill.
            self.add_dialog(SkillDialog(skill_dialog_options, skill_info.id))

    def _create_begin_activity(
        self, context: TurnContext, skill_id: str, selected_option: str
    ):
        if selected_option.lower() == JUST_FORWARD_THE_ACTIVITY.lower():
            # Note message activities also support input parameters but we are not using them in this example.
            # Return a deep clone of the activity so we don't risk altering the original one
            return copy.deepcopy(context.activity)

        # Get the begin activity from the skill instance.
        activity: Activity = self._skills_config.SKILLS[skill_id].create_begin_activity(
            selected_option
        )

        # We are manually creating the activity to send to the skill; ensure we add the ChannelData and Properties
        # from the original activity so the skill gets them.
        # Note: this is not necessary if we are just forwarding the current activity from context.
        activity.channel_data = context.activity.channel_data
        activity.additional_properties = context.activity.additional_properties

        return activity
