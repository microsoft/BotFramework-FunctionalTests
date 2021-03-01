# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from botframework.connector import Channels

from .card_options import CardOptions


UNSUPPORTED_CHANNEL_CARDS = {
    Channels.emulator.value: [CardOptions.teams_file_consent, CardOptions.o365]
}


class ChannelSupportedCards:
    @staticmethod
    def is_card_supported(channel: str, card_type: CardOptions):
        if channel in UNSUPPORTED_CHANNEL_CARDS:
            if card_type in UNSUPPORTED_CHANNEL_CARDS[channel]:
                return False
        return True
