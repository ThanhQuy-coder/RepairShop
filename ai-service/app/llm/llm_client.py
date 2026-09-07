from __future__ import annotations

import json

import httpx  # type: ignore

from app.core.config import settings


class LLMTimeoutError(Exception):
    """Ném ra khi LLM Provider không phản hồi trong REQUEST_TIMEOUT_SECONDS (Task 5 Tuần 2: timeout = 5s)."""


class LLMResponseError(Exception):
    """Ném ra khi LLM trả về lỗi HTTP hoặc nội dung không parse được thành JSON hợp lệ."""


class LLMClient:
    """
    Client gọi LLM Provider qua chuẩn OpenAI-compatible Chat Completions API — không gắn cứng
    1 vendor cụ thể, chỉ cần đổi LLM_BASE_URL/LLM_API_KEY/LLM_MODEL trong .env để đổi provider
    (OpenAI, Azure OpenAI, hoặc bất kỳ dịch vụ nào tương thích chuẩn này).
    """

    def __init__(self) -> None:
        self._base_url = settings.llm_base_url
        self._api_key = settings.llm_api_key
        self._model = settings.llm_model
        self._timeout = settings.request_timeout_seconds

    async def generate_json(self, system_prompt: str, user_prompt: str) -> dict:
        """Gọi LLM, ép trả về JSON hợp lệ (response_format=json_object), parse thành dict."""
        payload = {
            "model": self._model,
            "messages": [
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_prompt},
            ],
            "response_format": {"type": "json_object"},
            "temperature": 0.2,  # thấp — cần output ổn định, có cấu trúc, không "sáng tạo" ngoài phạm vi
        }

        headers = {
            "Authorization": f"Bearer {self._api_key}",
            "Content-Type": "application/json",
        }

        try:
            async with httpx.AsyncClient(timeout=self._timeout) as client:
                response = await client.post(
                    f"{self._base_url}/chat/completions", json=payload, headers=headers
                )
        except httpx.TimeoutException as exc:
            raise LLMTimeoutError(
                "LLM provider did not respond within timeout."
            ) from exc
        except httpx.HTTPError as exc:
            raise LLMResponseError(f"HTTP error calling LLM provider: {exc}") from exc

        if response.status_code >= 400:
            raise LLMResponseError(
                f"LLM provider returned {response.status_code}: {response.text}"
            )

        try:
            body = response.json()
            content = body["choices"][0]["message"]["content"]
            return json.loads(content)
        except (KeyError, IndexError, json.JSONDecodeError) as exc:
            raise LLMResponseError(
                f"Unable to parse LLM response as JSON: {exc}"
            ) from exc


llm_client = LLMClient()
