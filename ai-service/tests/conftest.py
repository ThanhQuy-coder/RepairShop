"""Fixture dùng chung cho test MAS (Task 2.1–2.3)."""

from __future__ import annotations

from typing import Any, Dict

import pytest

from tests.fixtures.context import (
    CORE_PARTS,
    CORE_SERVICES,
    CORE_SLOTS,
    REQUEST_ID,
    build_advisory_payload,
    build_agent_payload,
)
from tests.fixtures.llm import FakeLLMClient  # noqa: F401  (re-export cho test dùng)


@pytest.fixture
def services() -> list[dict]:
    return list(CORE_SERVICES)


@pytest.fixture
def fake_diagnosis_response() -> Dict[str, Any]:
    return {
        "outOfScope": False,
        "possibleCauses": [
            {"name": "Pin chai hoặc suy giảm dung lượng", "confidence": "HIGH"}
        ],
        "suggestedServiceIds": ["svc-pin"],
        "suggestedPartIds": ["part-pin-ip13"],
        "missingInformation": [],
        "explanation": "Mô tả phù hợp với khả năng pin chai.",
    }


@pytest.fixture
def agent_payload():
    """Hàm build payload MAS (mặc định đủ context + bật cả 3 agent)."""
    return build_agent_payload


@pytest.fixture
def advisory_payload():
    return build_advisory_payload


@pytest.fixture
def core_context() -> Dict[str, Any]:
    return {
        "services": CORE_SERVICES,
        "parts": CORE_PARTS,
        "slots": CORE_SLOTS,
        "requestId": REQUEST_ID,
    }
