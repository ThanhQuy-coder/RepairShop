"""Task 2.1 — API `POST /v1/agents/execute`: auth, validate, luồng offline."""

from __future__ import annotations

import pytest
from fastapi.testclient import TestClient

from app.core.config import settings
from app.main import app
from tests.fixtures.context import CORE_PARTS, build_agent_payload

client = TestClient(app)


def _headers() -> dict:
    return {"X-Internal-Api-Key": settings.internal_api_key}


def test_endpoint_requires_internal_api_key():
    response = client.post("/v1/agents/execute", json=build_agent_payload("Xin chào"))
    assert response.status_code in (401, 422)


def test_endpoint_rejects_wrong_api_key():
    response = client.post(
        "/v1/agents/execute",
        json=build_agent_payload("Xin chào"),
        headers={"X-Internal-Api-Key": "sai-key"},
    )
    assert response.status_code == 401


def test_unknown_intent_returns_guidance(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")
    payload = build_agent_payload("Kể chuyện cười", allow_quotation=False, allow_appointment=False)

    response = client.post("/v1/agents/execute", json=payload, headers=_headers())

    assert response.status_code == 200
    body = response.json()
    assert body["status"] == "SUCCESS"
    assert body["intent"] == "UNKNOWN"
    assert body["executedAgents"] == []
    assert "chưa xác định" in body["reason"]


def test_diagnosis_flow_over_http(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")
    payload = build_agent_payload(
        "Pin tụt nhanh",
        issue_description="Pin tụt nhanh, máy nóng",
        allow_quotation=False,
        allow_appointment=False,
    )

    response = client.post("/v1/agents/execute", json=payload, headers=_headers())

    assert response.status_code == 200
    body = response.json()
    assert body["status"] == "SUCCESS"
    assert body["diagnosis"]["suggestedServices"]
    service = body["diagnosis"]["suggestedServices"][0]
    assert service["serviceId"] in {"svc-pin", "svc-ve-sinh"}
    assert service["serviceName"]  # tên lấy từ context, không rỗng
    assert body["meta"]["llmCalls"] == 0
    assert body["meta"]["fallbackUsed"] is True


def test_cost_price_in_part_context_is_rejected():
    parts = [dict(CORE_PARTS[0], costPrice=200000)]
    payload = build_agent_payload("Pin tụt nhanh", parts=parts)

    response = client.post("/v1/agents/execute", json=payload, headers=_headers())

    assert response.status_code == 422  # fail-closed: không cho dữ liệu nội bộ lọt vào AI


def test_inconsistent_slot_is_rejected():
    slots = [
        {
            "timeSlotId": "slot-x",
            "slotStart": "09:00",
            "slotEnd": "10:00",
            "available": 0,
            "isFull": False,  # mâu thuẫn với available == 0
        }
    ]
    payload = build_agent_payload("Đặt lịch mai", slots=slots)

    response = client.post("/v1/agents/execute", json=payload, headers=_headers())

    assert response.status_code == 422


def test_blank_message_is_rejected():
    payload = build_agent_payload("   ")

    response = client.post("/v1/agents/execute", json=payload, headers=_headers())

    assert response.status_code == 422


def test_invalid_device_type_is_rejected():
    payload = build_agent_payload("Pin tụt nhanh", device={"deviceType": "tablet", "brand": "A", "model": "B"})

    response = client.post("/v1/agents/execute", json=payload, headers=_headers())

    assert response.status_code == 422


@pytest.mark.parametrize("field", ["actorType", "requestId", "device"])
def test_required_fields_are_enforced(field):
    payload = build_agent_payload("Pin tụt nhanh")
    payload.pop(field)

    response = client.post("/v1/agents/execute", json=payload, headers=_headers())

    assert response.status_code == 422


def test_full_consultation_flow_over_http(monkeypatch):
    from datetime import date

    from app.agents.appointment import agent as appointment_module
    from app.agents.appointment.agent import AppointmentAgent

    monkeypatch.setattr(settings, "llm_mode", "offline")
    monkeypatch.setattr(
        appointment_module,
        "_appointment_agent",
        AppointmentAgent(today_provider=lambda: date(2026, 11, 9)),
    )
    payload = build_agent_payload(
        "Pin chai rồi, báo giá và đặt lịch mai 3h chiều",
        issue_description="Pin chai",
    )

    response = client.post("/v1/agents/execute", json=payload, headers=_headers())

    assert response.status_code == 200
    body = response.json()
    assert body["status"] == "SUCCESS"
    assert body["executedAgents"] == ["DIAGNOSIS", "QUOTATION", "APPOINTMENT"]
    assert body["quotation"]["estimatedTotal"] > 0
    assert body["appointment"]["bookingDraft"]["timeSlotId"] == "slot-1500"
    assert body["appointment"]["requiresUserConfirmation"] is True
    assert body["appointment"].get("appointmentId") is None
