"""LLM giả dùng chung cho test — không gọi mạng, ghi lại prompt."""

from __future__ import annotations

from typing import Any, Dict


class FakeLLMClient:
    def __init__(self, response: Dict[str, Any]) -> None:
        self.response = response
        self.calls = 0
        self.last_system_prompt = ""
        self.last_user_prompt = ""

    async def generate_json(self, system_prompt: str, user_prompt: str) -> Dict[str, Any]:
        self.calls += 1
        self.last_system_prompt = system_prompt
        self.last_user_prompt = user_prompt
        return self.response
