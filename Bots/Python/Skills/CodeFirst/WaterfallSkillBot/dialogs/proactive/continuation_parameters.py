# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

from botbuilder.schema import ConversationReference


class ContinuationParameters:
    def __init__(
        self,
        claims_dentity: str,
        oauth_scope: str,
        conversation_reference: ConversationReference,
    ):
        self.claims_dentity = claims_dentity
        self.oauth_scope = oauth_scope
        self.conversation_reference = conversation_reference
