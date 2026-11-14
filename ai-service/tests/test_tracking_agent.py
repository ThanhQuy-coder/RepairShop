"""TASK-11 — Tracking Agent: tra cứu tiến độ đơn sửa chữa từ context."""

from __future__ import annotations

import pytest

from app.agents.tracking.agent import TrackingAgent
from app.schemas.state_models import AgentState


def _sample_lookup(ticket_code: str = "RS-20261109-001", status: str = "InRepair") -> dict:
    return {
        "ticketCode": ticket_code,
        "deviceLabel": "iPhone 13",
        "status": status,
        "statusLabel": "Đang sửa chữa" if status == "InRepair" else "Sẵn sàng bàn giao",
        "statusHistory": [
            {
                "status": "CheckedIn",
                "statusLabel": "Đã tiếp nhận",
                "changedAt": "2026-11-09T08:30:00+07:00",
            },
            {
                "status": status,
                "statusLabel": "Đang sửa chữa" if status == "InRepair" else "Sẵn sàng bàn giao",
                "changedAt": "2026-11-09T10:15:00+07:00",
            },
        ],
        "estimatedCompletion": None,
    }


def _base_state(
    message: str,
    *,
    lookups: list[dict] | None = None,
    ticket_code: str | None = None,
) -> AgentState:
    return {
        "request_id": "22222222-2222-2222-2222-222222222222",
        "user_message": message,
        "issue_description": message,
        "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"},
        "tracking_lookups": lookups or [],
        "ticket_code": ticket_code,
        "executed_agents": [],
        "timings": {},
        "errors": [],
    }


@pytest.mark.asyncio
async def test_tracking_exact_ticket_code_match():
    state = _base_state(
        "Tra cứu giúp tôi đơn RS-20261109-001",
        lookups=[_sample_lookup("RS-20261109-001")],
    )

    out = await TrackingAgent().run(state)
    res = out["tracking_result"]

    assert out["need_clarification"] is False
    assert out["ticket_code"] == "RS-20261109-001"
    assert len(res.tickets) == 1
    assert res.tickets[0].status == "InRepair"
    assert res.tickets[0].status_label == "Đang sửa chữa"
    assert len(res.tickets[0].status_history) == 2
    assert res.tickets[0].estimated_completion is None


@pytest.mark.asyncio
async def test_tracking_customer_multiple_tickets_without_code():
    state = _base_state(
        "Tiến độ sửa máy của tôi tới đâu rồi",
        lookups=[
            _sample_lookup("RS-20261109-001", "InRepair"),
            _sample_lookup("RS-20261105-010", "ReadyForPickup"),
        ],
    )

    out = await TrackingAgent().run(state)
    res = out["tracking_result"]

    assert out["need_clarification"] is False
    assert len(res.tickets) == 2
    assert out["ticket_code"] == "RS-20261109-001"
    assert "2 phiếu sửa chữa" in res.explanation


@pytest.mark.asyncio
async def test_tracking_unknown_ticket_code_returns_no_match():
    state = _base_state("Kiểm tra đơn RS-NOTFOUND")

    out = await TrackingAgent().run(state)
    res = out["tracking_result"]

    assert out["need_clarification"] is False
    assert res.tickets == []
    assert "Không tìm thấy" in res.explanation


@pytest.mark.asyncio
async def test_tracking_missing_code_requests_clarification():
    state = _base_state("Máy tôi sửa xong chưa ạ")

    out = await TrackingAgent().run(state)
    res = out["tracking_result"]

    assert out["need_clarification"] is True
    assert "ticketCode" in out["missing_information"]
    assert res.tickets == []
    assert "ticketCode" in res.missing_information
