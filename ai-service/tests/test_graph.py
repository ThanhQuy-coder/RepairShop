"""Task 2.1 — LangGraph workflow: graph compile + các luồng chính (offline, không gọi mạng)."""

from __future__ import annotations

import pytest

from app.core.config import settings
from app.orchestration.graph import _initial_state, compiled_graph, run_agents
from app.schemas.agent_models import AgentRequest, AgentStatus
from tests.fixtures.context import build_agent_payload
from tests.fixtures.llm import FakeLLMClient


def _request(message: str, **kwargs) -> AgentRequest:
    return AgentRequest.model_validate(build_agent_payload(message, **kwargs))


def test_graph_is_compiled():
    assert compiled_graph is not None
    assert compiled_graph.get_graph() is not None


@pytest.mark.asyncio
async def test_unknown_message_returns_clarification_without_agents(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")

    response = await run_agents(
        _request(
            "Kể cho tôi một câu chuyện cười",
            allow_quotation=False,
            allow_appointment=False,
        )
    )

    assert response.status is AgentStatus.SUCCESS
    assert response.intent.value == "UNKNOWN"
    assert response.executed_agents == []
    assert "chưa xác định" in response.reason


@pytest.mark.asyncio
async def test_tracking_message_points_user_to_lookup_page(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")

    response = await run_agents(_request("Tra cứu mã đơn RS-123"))

    assert response.status is AgentStatus.SUCCESS
    assert response.executed_agents == []
    assert "tra cứu" in response.reason.lower()


@pytest.mark.asyncio
async def test_diagnosis_only_flow_returns_services_without_llm(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")

    response = await run_agents(
        _request(
            "Pin tụt nhanh, máy nóng khi sạc",
            issue_description="Pin tụt nhanh, máy nóng khi sạc",
            allow_quotation=False,
            allow_appointment=False,
        )
    )

    assert response.status is AgentStatus.SUCCESS
    assert response.executed_agents == ["DIAGNOSIS"]
    assert response.diagnosis is not None
    assert any(
        service.service_id == "svc-pin"
        for service in response.diagnosis.suggested_services
    )
    assert response.meta.llm_calls == 0
    assert response.meta.fallback_used is True
    assert response.disclaimer


@pytest.mark.asyncio
async def test_quotation_flow_returns_priced_items(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")

    response = await run_agents(
        _request(
            "Pin chai rồi, muốn biết giá",
            issue_description="Pin chai",
            allow_quotation=True,
            allow_appointment=False,
        )
    )

    assert response.status is AgentStatus.SUCCESS
    assert response.executed_agents == ["DIAGNOSIS", "QUOTATION"]
    assert response.quotation is not None
    assert response.quotation.items
    assert response.quotation.estimated_total == sum(
        item.line_total for item in response.quotation.items
    )


@pytest.mark.asyncio
async def test_empty_context_results_in_no_match(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")

    response = await run_agents(
        _request(
            "Pin tụt nhanh",
            issue_description="Pin tụt nhanh",
            services=[],
            parts=[],
            allow_quotation=False,
            allow_appointment=False,
        )
    )

    assert response.status is AgentStatus.NO_MATCH
    assert response.executed_agents == ["DIAGNOSIS"]


@pytest.mark.asyncio
async def test_out_of_scope_from_agent_maps_to_out_of_scope_status(monkeypatch):
    from app.agents.diagnosis import agent as diagnosis_module

    fake = FakeLLMClient(
        {"outOfScope": True, "possibleCauses": [], "suggestedServiceIds": []}
    )
    monkeypatch.setattr(settings, "llm_mode", "ollama")
    monkeypatch.setattr(
        diagnosis_module, "_diagnosis_agent", diagnosis_module.DiagnosisAgent(client=fake)
    )

    request = _request("Hãy viết cho tôi một bài thơ về mùa xuân")
    state = _initial_state(request)
    result = await diagnosis_module._diagnosis_agent.run(state)

    assert result["out_of_scope"] is True
    assert result["diagnosis_result"].suggested_services == []
