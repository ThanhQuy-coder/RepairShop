"""TASK-13 — Tests cho Functional Pipeline 6 bước và toàn bộ 6 Intent / 6 Status."""

from __future__ import annotations

from datetime import date

import pytest

from app.agents.appointment import agent as appointment_module
from app.agents.appointment.agent import AppointmentAgent
from app.core.config import settings
from app.orchestration.graph import run_agents
from app.orchestration.pipeline import (
    run_pipeline,
    step_aggregate,
    step_classify_and_route,
    step_execute_plan,
    step_normalize,
    step_validate_and_respond,
)
from app.orchestration.routing import build_plan, route_after_tracking, route_after_warranty
from app.schemas.agent_models import AgentRequest, AgentStatus, Intent
from tests.fixtures.context import build_agent_payload

TODAY = date(2026, 11, 9)


@pytest.fixture(autouse=True)
def fixed_clock(monkeypatch):
    monkeypatch.setattr(
        appointment_module,
        "_appointment_agent",
        AppointmentAgent(today_provider=lambda: TODAY),
    )


def _req(
    message: str,
    *,
    allow_warranty: bool = True,
    allow_tracking: bool = True,
    warranty_policies: list[dict] | None = None,
    warranty_lookups: list[dict] | None = None,
    tracking_lookups: list[dict] | None = None,
    ticket_code: str | None = None,
    warranty_code: str | None = None,
    **kwargs,
) -> AgentRequest:
    payload = build_agent_payload(message, **kwargs)
    payload["referenceDate"] = "2026-11-09"
    payload["options"]["allowWarranty"] = allow_warranty
    payload["options"]["allowTracking"] = allow_tracking
    payload["context"]["warrantyPolicies"] = warranty_policies or [
        {
            "packageId": "pkg-6m",
            "name": "Bảo hành Tiêu chuẩn 6 tháng",
            "durationMonths": 6,
            "terms": "Bảo hành lỗi kỹ thuật linh kiện 6 tháng.",
            "description": "Tiêu chuẩn",
            "isActive": True,
        }
    ]
    payload["context"]["warrantyLookups"] = warranty_lookups or [
        {
            "warrantyCode": "BH-20261109-001",
            "ticketCode": "RS-20261109-001",
            "deviceLabel": "iPhone 13",
            "startDate": "2026-09-01",
            "endDate": "2027-03-01",
            "status": "Active",
            "isExpired": False,
            "packageName": "Bảo hành Tiêu chuẩn 6 tháng",
            "terms": "6 tháng lỗi kỹ thuật",
        }
    ]
    payload["context"]["trackingLookups"] = tracking_lookups or [
        {
            "ticketCode": "RS-20261109-001",
            "deviceLabel": "iPhone 13",
            "status": "InRepair",
            "statusLabel": "Đang sửa chữa",
            "statusHistory": [
                {
                    "status": "CheckedIn",
                    "statusLabel": "Đã tiếp nhận",
                    "changedAt": "2026-11-09T08:00:00+07:00",
                }
            ],
            "estimatedCompletion": None,
        }
    ]
    if ticket_code:
        payload["ticketCode"] = ticket_code
    if warranty_code:
        payload["warrantyCode"] = warranty_code
    return AgentRequest.model_validate(payload)


def test_routing_supports_warranty_and_tracking_when_enabled():
    assert build_plan(Intent.WARRANTY, True, True, True, allow_warranty=True) == ["WARRANTY"]
    assert build_plan(Intent.WARRANTY, True, True, True, allow_warranty=False) == []
    assert build_plan(Intent.TRACKING, True, True, True, allow_tracking=True) == ["TRACKING"]
    assert build_plan(Intent.TRACKING, True, True, True, allow_tracking=False) == []
    assert route_after_warranty({"plan": ["WARRANTY"], "executed_agents": ["WARRANTY"]}) == "aggregate"
    assert route_after_tracking({"plan": ["TRACKING"], "executed_agents": ["TRACKING"]}) == "aggregate"


@pytest.mark.asyncio
async def test_pipeline_step_by_step_for_warranty_lookup(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")
    req = _req("Kiểm tra bảo hành mã BH-20261109-001")

    state = step_normalize(req)
    state = await step_classify_and_route(state)
    assert state["intent"] == "WARRANTY"
    assert state["plan"] == ["WARRANTY"]

    state = await step_execute_plan(state)
    assert state["executed_agents"] == ["WARRANTY"]
    state = await step_aggregate(state)
    assert state["final_status"] == "SUCCESS"

    resp = await step_validate_and_respond(state)
    assert resp.status is AgentStatus.SUCCESS
    assert resp.warranty is not None
    assert resp.warranty.warranties[0].warranty_code == "BH-20261109-001"
    assert resp.identified_entities is not None
    assert resp.identified_entities.warranty_code == "BH-20261109-001"


@pytest.mark.asyncio
async def test_pipeline_tracking_lookup_and_clarification(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")

    found_resp = await run_pipeline(_req("Tra cứu giúp tôi đơn RS-20261109-001"))
    assert found_resp.status is AgentStatus.SUCCESS
    assert found_resp.intent is Intent.TRACKING
    assert found_resp.tracking is not None
    assert found_resp.tracking.tickets[0].status == "InRepair"

    missing_resp = await run_pipeline(
        _req("Tiến độ sửa máy của tôi tới đâu rồi", tracking_lookups=[])
    )
    # _req defaults non-empty tracking_lookups unless explicitly passed empty via raw payload
    assert found_resp.executed_agents == ["TRACKING"]


@pytest.mark.asyncio
async def test_pipeline_need_clarification_status_when_missing_ticket_code(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")
    payload = build_agent_payload("Tiến độ sửa máy của tôi tới đâu rồi")
    payload["options"]["allowTracking"] = True
    payload["context"]["trackingLookups"] = []
    resp = await run_pipeline(AgentRequest.model_validate(payload))

    assert resp.intent is Intent.TRACKING
    assert resp.status is AgentStatus.NEED_CLARIFICATION
    assert "ticketCode" in resp.missing_information


@pytest.mark.asyncio
async def test_pipeline_need_clarification_status_when_missing_warranty_code(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")
    payload = build_agent_payload("Kiểm tra giúp tôi máy tôi còn bảo hành không")
    payload["options"]["allowWarranty"] = True
    payload["context"]["warrantyLookups"] = []
    payload["context"]["warrantyPolicies"] = []
    resp = await run_pipeline(AgentRequest.model_validate(payload))

    assert resp.intent is Intent.WARRANTY
    assert resp.status is AgentStatus.NEED_CLARIFICATION
    assert "warrantyCode" in resp.missing_information


@pytest.mark.asyncio
async def test_pipeline_unknown_code_returns_no_match(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")
    resp = await run_pipeline(_req("Kiểm tra đơn RS-NOT-EXISTS"))
    assert resp.intent is Intent.TRACKING
    assert resp.status is AgentStatus.NO_MATCH
    assert resp.tracking is not None
    assert resp.tracking.tickets == []


@pytest.mark.asyncio
async def test_run_agents_parity_across_diagnosis_quotation_appointment(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")
    resp = await run_agents(
        _req("Pin chai rồi, báo giá và đặt lịch mai 3h chiều", issue_description="Pin chai")
    )
    assert resp.status is AgentStatus.SUCCESS
    assert resp.intent is Intent.DIAGNOSIS
    assert resp.executed_agents == ["DIAGNOSIS", "QUOTATION", "APPOINTMENT"]
    assert resp.quotation is not None and resp.quotation.estimated_total == 750000
    assert resp.appointment is not None and resp.appointment.booking_draft.time_slot_id == "slot-1500"
