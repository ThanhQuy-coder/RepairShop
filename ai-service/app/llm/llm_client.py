from __future__ import annotations

import json
import logging
import re
from typing import Any, Optional

import httpx  # type: ignore

from app.core.config import settings

logger = logging.getLogger(__name__)

_FENCE_PATTERN = re.compile(
    r"^\s*```(?:json|JSON)?\s*(?P<body>.*?)\s*```\s*$", re.DOTALL
)


class LLMTimeoutError(Exception):
    """Ném ra khi LLM Provider không phản hồi trong REQUEST_TIMEOUT_SECONDS (Task 5 Tuần 2: timeout = 5s)."""


class LLMResponseError(Exception):
    """Ném ra khi LLM trả về lỗi HTTP hoặc nội dung không parse được thành JSON hợp lệ."""


def strip_code_fence(content: str) -> str:
    """
    Bóc khối ```json ... ``` mà nhiều LLM vẫn trả về dù prompt đã dặn.

    Sửa D-02 (Task 2.2): trước đây `json.loads(content)` gặp fence là ném JSONDecodeError
    -> 500 AI_INTERNAL_ERROR oan uổng.
    """
    match = _FENCE_PATTERN.match(content)
    return (match.group("body") if match else content).strip()


def _extract_json_object(content: str) -> Optional[dict]:
    """Fallback: cắt từ '{' đầu tiên tới '}' cuối cùng nếu model thêm lời dẫn."""
    start = content.find("{")
    end = content.rfind("}")
    if start == -1 or end <= start:
        return None
    try:
        parsed = json.loads(content[start : end + 1])
    except json.JSONDecodeError:
        return None
    return parsed if isinstance(parsed, dict) else None


class LLMClient:
    """
    Client gọi Ollama qua chuẩn OpenAI-compatible Chat Completions API.
    AI Service chỉ dùng Ollama local: `LLM_BASE_URL` bị validator chặn nếu không phải host local
    (xem `app/core/config.py`).

    Task 2.2: dùng lại MỘT `httpx.AsyncClient` cho nhiều request thay vì tạo mới mỗi lần
    (tiết kiệm ~0.4s bắt tay/kết nối đo được ở Task 1.3).
    """

    def __init__(self) -> None:
        self._base_url = settings.llm_base_url
        self._api_key = settings.llm_api_key
        self._model = settings.llm_model
        self._timeout = settings.request_timeout_seconds
        self._client: Optional[httpx.AsyncClient] = None

    def _get_client(self) -> httpx.AsyncClient:
        if self._client is None or self._client.is_closed:
            self._client = httpx.AsyncClient(timeout=self._timeout)
        return self._client

    async def aclose(self) -> None:
        if self._client is not None and not self._client.is_closed:
            await self._client.aclose()

    async def generate_json(self, system_prompt: str, user_prompt: str) -> dict:
        payload = {
            "model": self._model,
            "messages": [
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_prompt},
            ],
            "temperature": 0.2,
        }
        headers = {
            "Authorization": f"Bearer {self._api_key}",
            "Content-Type": "application/json",
        }
        url = f"{self._base_url.rstrip('/')}/chat/completions"

        try:
            response = await self._get_client().post(url, json=payload, headers=headers)
        except httpx.TimeoutException as exc:
            logger.warning("LLM timeout sau %ss (model=%s)", self._timeout, self._model)
            raise LLMTimeoutError("LLM provider did not respond within timeout.") from exc
        except httpx.HTTPError as first_exc:
            logger.warning("Lỗi HTTP tạm thời khi gọi LLM (%s) — thử lại 1 lần.", type(first_exc).__name__)
            try:
                response = await self._get_client().post(url, json=payload, headers=headers)
            except httpx.TimeoutException as exc:
                logger.warning("LLM timeout sau %ss (model=%s)", self._timeout, self._model)
                raise LLMTimeoutError("LLM provider did not respond within timeout.") from exc
            except httpx.HTTPError as exc:
                logger.error("Lỗi HTTP khi gọi LLM: %s", type(exc).__name__)
                raise LLMResponseError(f"HTTP error calling LLM provider: {exc}") from exc

        if response.status_code >= 400:
            logger.warning(
                "LLM provider trả HTTP %s (model=%s): %s",
                response.status_code,
                self._model,
                response.text[:200],
            )
            raise LLMResponseError(
                f"LLM provider returned {response.status_code}: {response.text[:300]}"
            )

        try:
            body = response.json()
            content = body["choices"][0]["message"]["content"]
        except (KeyError, IndexError, ValueError) as exc:
            raise LLMResponseError(
                f"Unable to read LLM response payload: {type(exc).__name__}"
            ) from exc

        if isinstance(content, dict):  # một số provider trả object thay vì string
            return content
        if not isinstance(content, str):
            raise LLMResponseError("LLM content is not a string or object.")

        raw = strip_code_fence(content)
        try:
            parsed: Any = json.loads(raw)
        except json.JSONDecodeError:
            extracted = _extract_json_object(raw)
            if extracted is None:
                logger.warning("LLM content không phải JSON hợp lệ: %s", raw[:200])
                raise LLMResponseError("Unable to parse LLM response as JSON.")
            parsed = extracted

        if not isinstance(parsed, dict):
            raise LLMResponseError("LLM JSON response must be an object.")

        logger.debug("LLM JSON ok (model=%s, %d keys)", self._model, len(parsed))
        return parsed


llm_client = LLMClient()
