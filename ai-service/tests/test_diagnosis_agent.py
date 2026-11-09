"""Task 2.2 — Diagnosis Agent: mapping ID, fallback rule, defensive parsing, timeout."""

from __future__ import annotations

import pytest

from app.agents.diagnosis.agent import DiagnosisAgent
from app.core.config import settings
from app.llm.llm_client import (
    LLMResponseError,
    LLMTimeoutError,
    _extract_json_object,
    strip_code_fence,
)
from app.orchestration.graph import _initial_state
from app.schemas.agent_models import AgentRequest
from tests.fixtures.context import build_agent_payload
from tests.fixtures.llm import FakeLLMClient


def _state(
    message: str = "Pin tụt nhanh",
    issue: str = "Pin tụt nhanh, máy nóng",
    **kwargs,
):
    payload = build_agent_payload(message, issue_description=issue, **kwargs)
    return _initial_state(AgentRequest.model_validate(payload))


def test_strip_code_fence_removes_markdown_wrapper():
    raw = '```json\n{"possibleCauses": []}\n```'
    assert strip_code_fence(raw) == '{"possibleCauses": []}'

    plain = '{"possibleCauses": []}'
    assert strip_code_fence(plain) == plain


def test_extract_json_object_ignores_leading_text():
    assert _extract_json_object('Đây là kết quả: {"a": 1}') == {"a": 1}
    assert _extract_json_object("không có json") is None


@pytest.mark.asyncio
async def test_offline_mode_uses_rule_based_diagnosis_without_llm(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")
    fake = FakeLLMClient({"unused": True})
    agent = DiagnosisAgent(client=fake)

    result = await agent.run(_state())

    diagnosis = result["diagnosis_result"]
    assert diagnosis.suggested_services
    assert any(service.service_id == "svc-pin" for service in diagnosis.suggested_services)
    assert diagnosis.possible_causes
    assert result["llm_calls"] == 0
    assert result["fallback_used"] is True
    assert fake.calls == 0


@pytest.mark.asyncio
async def test_llm_ids_are_mapped_from_real_context(monkeypatch, fake_diagnosis_response):
    monkeypatch.setattr(settings, "llm_mode", "ollama")
    fake = FakeLLMClient(fake_diagnosis_response)
    agent = DiagnosisAgent(client=fake)

    result = await agent.run(_state())

    diagnosis = result["diagnosis_result"]
    assert [service.service_id for service in diagnosis.suggested_services] == ["svc-pin"]
    assert diagnosis.suggested_services[0].service_name == "Thay pin"
    assert [part.part_id for part in diagnosis.suggested_parts] == ["part-pin-ip13"]
    assert diagnosis.suggested_parts[0].part_name == "Pin iPhone 13"
    assert result["llm_calls"] == 1
    assert result["fallback_used"] is False
    assert fake.calls == 1


@pytest.mark.asyncio
async def test_hallucinated_ids_are_discarded_and_rule_fallback_fills_from_context(
    monkeypatch,
):
    monkeypatch.setattr(settings, "llm_mode", "ollama")
    fake = FakeLLMClient(
        {
            "outOfScope": False,
            "possibleCauses": [{"name": "Lỗi lạ", "confidence": "HIGH"}],
            "suggestedServiceIds": ["svc-fake-chip-abc"],
            "suggestedPartIds": ["part-fake"],
            "missingInformation": [],
            "explanation": "Đề xuất linh kiện không có trong danh mục.",
        }
    )
    agent = DiagnosisAgent(client=fake)

    result = await agent.run(_state())

    ids = {service.service_id for service in result["diagnosis_result"].suggested_services}
    assert "svc-fake-chip-abc" not in ids
    assert ids  # fallback rule lấy từ context thật
    assert ids.issubset(
        {
            "svc-pin",
            "svc-man-hinh",
            "svc-cam-ung",
            "svc-ve-sinh",
            "svc-sac",
            "svc-loa",
            "svc-camera",
        }
    )
    assert result["fallback_used"] is True


@pytest.mark.asyncio
async def test_duplicate_ids_from_llm_are_deduplicated(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "ollama")
    fake = FakeLLMClient(
        {
            "outOfScope": False,
            "possibleCauses": [{"name": "Pin chai", "confidence": "HIGH"}],
            "suggestedServiceIds": ["svc-pin", "svc-pin", "svc-sac"],
            "suggestedPartIds": ["part-pin-ip13", "part-pin-ip13"],
            "missingInformation": [],
            "explanation": "Pin chai.",
        }
    )

    result = await DiagnosisAgent(client=fake).run(_state())

    services = result["diagnosis_result"].suggested_services
    parts = result["diagnosis_result"].suggested_parts
    assert [service.service_id for service in services] == ["svc-pin", "svc-sac"]
    assert [part.part_id for part in parts] == ["part-pin-ip13"]


@pytest.mark.asyncio
async def test_llm_out_of_scope_flag(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "ollama")
    fake = FakeLLMClient({"outOfScope": True, "possibleCauses": []})
    agent = DiagnosisAgent(client=fake)

    result = await agent.run(_state("Hãy viết cho tôi một bài thơ"))

    assert result["out_of_scope"] is True
    assert result["diagnosis_result"].suggested_services == []
    assert result["diagnosis_result"].suggested_parts == []


@pytest.mark.asyncio
async def test_llm_parse_error_falls_back_to_rules(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "ollama")

    class BrokenClient:
        async def generate_json(self, system_prompt: str, user_prompt: str) -> dict:
            raise LLMResponseError("bad json")

    result = await DiagnosisAgent(client=BrokenClient()).run(_state())

    assert result["diagnosis_result"].suggested_services
    assert result["fallback_used"] is True
    assert any(error.code == "LLM_RESPONSE_INVALID" for error in result["errors"])


@pytest.mark.asyncio
async def test_llm_timeout_propagates_for_backend_fallback(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "ollama")

    class TimeoutClient:
        async def generate_json(self, system_prompt: str, user_prompt: str) -> dict:
            raise LLMTimeoutError("timeout")

    with pytest.raises(LLMTimeoutError):
        await DiagnosisAgent(client=TimeoutClient()).run(_state())


@pytest.mark.asyncio
async def test_empty_context_returns_missing_information(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")

    result = await DiagnosisAgent(client=FakeLLMClient({})).run(
        _state(services=[], parts=[])
    )

    diagnosis = result["diagnosis_result"]
    assert diagnosis.suggested_services == []
    assert diagnosis.missing_information


@pytest.mark.asyncio
async def test_explanation_is_reference_only_and_never_claims_certainty(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")

    result = await DiagnosisAgent(client=FakeLLMClient({})).run(_state())
    explanation = result["diagnosis_result"].explanation.lower()

    assert len(explanation) <= 300
    assert "chắc chắn" not in explanation
    assert "100%" not in explanation
