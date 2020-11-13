from abc import ABC
from botframework.connector.auth import ChannelProvider

class GovChannelProvider(ChannelProvider):
    async def get_channel_service(self) -> str:
        return "https://botframework.azure.us"

    async def is_government(self) -> bool:
        return True
    
    async def is_public_azure(self) -> bool:
        return False
    