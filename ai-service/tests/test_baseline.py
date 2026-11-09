"""Task 4.2 — Single-Agent baseline: schema, mapping, fallback và endpoint."""

from __future__ import annotations

from datetime import date

import pytest
from fastapi.testclient import TestClient

from app.baseline import single_agent as baseline_module
from app.baseline.single_agent import SingleAgentBaseline
from app.core.config import settings
from app.llm.llm_client import LLMResponseError, LLMTimeoutError
from app.main import app
from app.schemas.agent_models import AgentRequest, AgentStatus
from tests.fixtures.context import build_agent_payload
from tests.fixtures.llm import FakeLLMClient

TODAY = date(2026, 11, 9)
client = TestClient(app)


def _request(message: str, **kwargs) -> AgentRequest:
    return AgentRequest.model_validate(build_agent_payload(message, **kwargs))


def _headers() -> dict:
    return {"X-Internal-Api-Key": settings.internal_api_key}


@pytest.mark.asyncio
async def test_offline_baseline_returns_full_single_agent_result(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")
    agent = SingleAgentBaseline(today_provider=lambda: TODAY)

    response = await agent.run(
        _request(
            "Pin chai rồi, báo giá và đặt lịch mai 3h chiều",
            issue_description="Pin chai",
        )
    )

    assert response.status is AgentStatus.SUCCESS
    assert [agent_name.value for agent_name in response.executed_agents] == ["SINGLE_AGENT"]
    assert response.diagnosis is not None
    assert response.quotation is not None
    assert response.quotation.estimated_total == 400000 + 350000
    assert response.appointment is not None
    assert response.appointment.booking_draft.time_slot_id == "slot-1500"
    assert response.appointment.requires_user_confirmation is True
    assert response.meta.llm_calls == 0
    assert response.meta.fallback_used is True


@pytest.mark.asyncio
async def test_ollama_baseline_uses_one_llm_call_for_everything():
    fake = FakeLLMClient(
        {
            "outOfScope": False,
            "possibleCauses": [{"name": "Pin chai", "confidence": "HIGH"}],
            "serviceIds": ["svc-pin"],
            "partIds": ["part-pin-ip13"],
            "slotId": "slot-1500",
            "missingInformation": [],
            "explanation": "Pin có thể đã chai; đã chọn khung giờ còn trống.",
        }
    )
    agent = SingleAgentBaseline(client=fake, today_provider=lambda: TODAY)

    response = await agent.run(
        _request("Pin chai, báo giá và đặt lịch mai 3h chiều", issue_description="Pin chai")
    )

    assert fake.calls == 1
    assert response.meta.llm_calls == 1
    assert response.meta.fallback_used is False
    assert [agent_name.value for agent_name in response.executed_agents] == ["SINGLE_AGENT"]
    assert response.diagnosis.suggested_services[0].service_id == "svc-pin"
    assert response.quotation.estimated_total == 750000
    assert response.appointment.booking_draft.time_slot_id == "slot-1500"


@pytest.mark.asyncio
async def test_baseline_drops_ids_outside_context_and_invalid_slot():
    fake = FakeLLMClient(
        {
            "outOfScope": False,
            "possibleCauses": [],
            "serviceIds": ["svc-fake"],
            "partIds": ["part-fake"],
            "slotId": "slot-fake",
            "missingInformation": [],
            "explanation": "Đề xuất không có trong danh mục.",
        }
    )
    agent = SingleAgentBaseline(client=fake, today_provider=lambda: TODAY)

    response = await agent.run(_request("Pin chai", issue_description="Pin chai"))

    assert response.diagnosis is not None
    assert response.diagnosis.suggested_services == []
    assert response.diagnosis.suggested_parts == []
    assert response.appointment is not None
    assert response.appointment.booking_draft.time_slot_id is None
    assert all(
        slot.time_slot_id != "slot-fake" for slot in response.appointment.suggested_slots
    )


@pytest.mark.asyncio
async def test_baseline_out_of_scope():
    fake = FakeLLMClient({"outOfScope": True})
    agent = SingleAgentBaseline(client=fake, today_provider=lambda: TODAY)

    # Câu có tín hiệu sửa chữa để baseline gọi LLM; LLM mới là bên báo out-of-scope.
    response = await agent.run(
        _request("Máy tôi bị lỗi, viết giúp tôi bài thơ", issue_description="máy bị lỗi")
    )

    assert response.status is AgentStatus.OUT_OF_SCOPE
    assert response.executed_agents == []


@pytest.mark.asyncio
async def test_baseline_invalid_json_falls_back(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "ollama")

    class BrokenClient:
        async def generate_json(self, system_prompt: str, user_prompt: str) -> dict:
            raise LLMResponseError("bad json")

    agent = SingleAgentBaseline(client=BrokenClient(), today_provider=lambda: TODAY)

    response = await agent.run(_request("Pin chai", issue_description="Pin chai"))

    # Có lỗi LLM nhưng vẫn có kết quả fallback => PARTIAL (không giấu lỗi).
    assert response.status is AgentStatus.PARTIAL
    assert response.meta.fallback_used is True


@pytest.mark.asyncio
async def test_baseline_timeout_propagates(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "ollama")

    class TimeoutClient:
        async def generate_json(self, system_prompt: str, user_prompt: str) -> dict:
            raise LLMTimeoutError("timeout")

    agent = SingleAgentBaseline(client=TimeoutClient(), today_provider=lambda: TODAY)

    with pytest.raises(LLMTimeoutError):
        await agent.run(_request("Pin chai", issue_description="Pin chai"))


def test_baseline_endpoint_requires_internal_key():
    response = client.post("/v1/baseline/execute", json=build_agent_payload("Xin chào"))
    assert response.status_code in (401, 422)


def test_baseline_endpoint_returns_same_contract(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")
    monkeypatch.setattr(
        baseline_module,
        "_single_agent_baseline",
        SingleAgentBaseline(today_provider=lambda: TODAY),
    )

    response = client.post(
        "/v1/baseline/execute",
        json=build_agent_payload(
            "Pin chai rồi, báo giá và đặt lịch mai 3h chiều",
            issue_description="Pin chai",
        ),
        headers=_headers(),
    )

    assert response.status_code == 200
    body = response.json()
    for key in (
        "requestId",
        "status",
        "intent",
        "executedAgents",
        "diagnosis",
        "quotation",
        "appointment",
        "reason",
        "meta",
    ):
        assert key in body
    assert body["executedAgents"] == ["SINGLE_AGENT"]
