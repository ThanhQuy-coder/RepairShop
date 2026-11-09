"""Task 3.3 — Luồng phối hợp đầy đủ: Diagnosis → Quotation → Appointment."""

from __future__ import annotations

from datetime import date

import pytest

from app.agents.appointment import agent as appointment_module
from app.agents.appointment.agent import AppointmentAgent
from app.core.config import settings
from app.orchestration.graph import run_agents
from app.schemas.agent_models import AgentRequest, AgentStatus
from tests.fixtures.context import build_agent_payload

TODAY = date(2026, 11, 9)


@pytest.fixture(autouse=True)
def fixed_appointment_clock(monkeypatch):
    """Cố định 'hôm nay' để test 'mai' không phụ thuộc ngày chạy thật."""
    monkeypatch.setattr(
        appointment_module,
        "_appointment_agent",
        AppointmentAgent(today_provider=lambda: TODAY),
    )


def _request(message: str, **kwargs) -> AgentRequest:
    return AgentRequest.model_validate(build_agent_payload(message, **kwargs))


@pytest.mark.asyncio
async def test_full_flow_diagnosis_quotation_appointment(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")

    response = await run_agents(
        _request(
            "Pin chai rồi, báo giá và đặt lịch mai 3h chiều",
            issue_description="Pin chai",
        )
    )

    assert response.status is AgentStatus.SUCCESS
    assert response.executed_agents == ["DIAGNOSIS", "QUOTATION", "APPOINTMENT"]

    assert response.diagnosis is not None
    assert any(
        service.service_id == "svc-pin"
        for service in response.diagnosis.suggested_services
    )

    assert response.quotation is not None
    assert response.quotation.estimated_total == 400000 + 350000
    assert {item.item_type for item in response.quotation.items} == {"SERVICE", "PART"}

    assert response.appointment is not None
    assert response.appointment.booking_draft is not None
    assert response.appointment.booking_draft.appointment_date == "2026-11-10"
    assert response.appointment.booking_draft.time_slot_id == "slot-1500"
    assert response.appointment.requires_user_confirmation is True
    assert "fullName" in response.appointment.missing_information

    assert "Giá tham khảo" in response.reason
    assert "khung giờ" in response.reason.lower()


@pytest.mark.asyncio
async def test_appointment_only_flow(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")

    response = await run_agents(
        _request(
            "Mai 3h chiều còn chỗ không",
            allow_diagnosis=False,
            allow_quotation=False,
        )
    )

    assert response.status is AgentStatus.SUCCESS
    assert response.intent.value == "APPOINTMENT"
    assert response.executed_agents == ["APPOINTMENT"]
    assert response.diagnosis is None
    assert response.quotation is None
    assert response.appointment is not None
    assert [slot.time_slot_id for slot in response.appointment.suggested_slots] == [
        "slot-1500"
    ]


@pytest.mark.asyncio
async def test_quotation_only_flow(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")

    response = await run_agents(
        _request(
            "Báo giá giúp tôi dịch vụ thay pin",
            allow_appointment=False,
        )
    )

    assert response.status is AgentStatus.SUCCESS
    assert response.intent.value == "QUOTATION"
    assert response.executed_agents == ["QUOTATION"]
    assert response.diagnosis is None
    assert response.quotation is not None
    assert [item.item_id for item in response.quotation.items] == ["svc-pin"]
    assert response.quotation.estimated_total == 400000


@pytest.mark.asyncio
async def test_missing_context_returns_no_match(monkeypatch):
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


@pytest.mark.asyncio
async def test_quotation_failure_gives_partial_with_diagnosis(monkeypatch):
    from app.agents.quotation import agent as quotation_module

    class BrokenQuotationAgent:
        async def run(self, state):
            raise RuntimeError("boom")

    monkeypatch.setattr(settings, "llm_mode", "offline")
    monkeypatch.setattr(quotation_module, "_quotation_agent", BrokenQuotationAgent())

    response = await run_agents(
        _request(
            "Pin chai rồi, báo giá giúp tôi",
            issue_description="Pin chai",
            allow_appointment=False,
        )
    )

    assert response.status is AgentStatus.PARTIAL
    assert response.diagnosis is not None
    assert response.executed_agents == ["DIAGNOSIS", "QUOTATION"]


@pytest.mark.asyncio
async def test_appointment_failure_without_other_result_is_error(monkeypatch):
    from app.agents.appointment import agent as appointment_agent_module

    class BrokenAppointmentAgent:
        async def run(self, state):
            raise RuntimeError("boom")

    monkeypatch.setattr(settings, "llm_mode", "offline")
    monkeypatch.setattr(
        appointment_agent_module, "_appointment_agent", BrokenAppointmentAgent()
    )

    response = await run_agents(
        _request(
            "Mai 3h chiều còn chỗ không",
            allow_diagnosis=False,
            allow_quotation=False,
        )
    )

    assert response.status is AgentStatus.ERROR
    assert response.appointment is None


@pytest.mark.asyncio
async def test_all_agent_results_are_traceable_in_meta(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")

    response = await run_agents(
        _request(
            "Pin chai rồi, báo giá và đặt lịch mai 3h chiều",
            issue_description="Pin chai",
        )
    )

    timings = response.meta.agent_timings_ms
    assert {"COORDINATOR", "DIAGNOSIS", "QUOTATION", "APPOINTMENT"} <= set(timings)
    assert response.meta.llm_calls == 0  # offline mode
    assert response.meta.fallback_used is True
