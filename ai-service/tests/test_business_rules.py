"""Task 3.4 — Kiểm thử nghiệp vụ: agent không tạo dữ liệu, không vượt quyền Backend."""

from __future__ import annotations

from datetime import date

import pytest
from pydantic import ValidationError

from app.agents.appointment import agent as appointment_module
from app.agents.appointment.agent import AppointmentAgent
from app.agents.quotation.agent import QuotationAgent
from app.core.config import settings
from app.orchestration.graph import _initial_state, run_agents
from app.orchestration.postprocess import validate_node
from app.schemas.agent_models import (
    AgentRequest,
    AgentResponse,
    AgentStatus,
    AppointmentResult,
    DiagnosisResult,
    QuotationItem,
    QuotationResult,
    SuggestedServiceOut,
    SuggestedSlot,
)
from tests.fixtures.context import CORE_SLOTS, build_agent_payload

TODAY = date(2026, 11, 9)


@pytest.fixture(autouse=True)
def fixed_appointment_clock(monkeypatch):
    monkeypatch.setattr(
        appointment_module,
        "_appointment_agent",
        AppointmentAgent(today_provider=lambda: TODAY),
    )


def _request(message: str, **kwargs) -> AgentRequest:
    return AgentRequest.model_validate(build_agent_payload(message, **kwargs))


# ── B1: giá thay đổi theo Backend, không hard-code --------------------------------


@pytest.mark.asyncio
async def test_b1_price_follows_backend_context(monkeypatch):
    from tests.fixtures.context import CORE_SERVICES

    monkeypatch.setattr(settings, "llm_mode", "offline")
    services = [dict(CORE_SERVICES[0], basePrice=555000)]

    response = await run_agents(
        _request(
            "Báo giá giúp tôi dịch vụ thay pin",
            services=services,
            allow_appointment=False,
        )
    )

    assert response.quotation is not None
    assert response.quotation.estimated_total == 555000


# ── B2: slot đầy không được gợi ý ------------------------------------------------


@pytest.mark.asyncio
async def test_b2_full_slot_is_never_suggested(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")

    response = await run_agents(
        _request(
            "Mai 14h còn chỗ không",
            allow_diagnosis=False,
            allow_quotation=False,
        )
    )

    assert response.appointment is not None
    ids = [slot.time_slot_id for slot in response.appointment.suggested_slots]
    assert "slot-1400" not in ids


# ── B3: ngày trong quá khứ bị từ chối mềm ---------------------------------------


@pytest.mark.asyncio
async def test_b3_past_date_is_rejected(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")

    response = await run_agents(
        _request(
            "Hôm qua còn chỗ không",
            allow_diagnosis=False,
            allow_quotation=False,
        )
    )

    assert response.status is AgentStatus.NO_MATCH
    assert response.appointment is not None
    assert response.appointment.suggested_slots == []
    assert "appointmentDate" in response.appointment.missing_information


# ── B4: ID không tồn tại bị loại -------------------------------------------------


@pytest.mark.asyncio
async def test_b4_unknown_service_id_is_discarded_by_validation():
    state = _initial_state(_request("Pin tụt nhanh", allow_quotation=False))
    state["plan"] = ["DIAGNOSIS"]
    state["executed_agents"] = ["DIAGNOSIS"]
    state["diagnosis_result"] = DiagnosisResult(
        suggestedServices=[
            SuggestedServiceOut(serviceId="svc-fake", serviceName="Dịch vụ bịa")
        ]
    )

    result = await validate_node(state)
    response = result["final_response"]

    assert response.status is AgentStatus.NO_MATCH
    assert response.diagnosis.suggested_services == []


# ── B5: linh kiện hết hàng không được báo giá ------------------------------------


@pytest.mark.asyncio
async def test_b5_out_of_stock_part_is_not_quoted():
    from app.schemas.agent_models import SuggestedPartOut

    state = _initial_state(_request("Thay màn hình", allow_quotation=False))
    state["diagnosis_result"] = DiagnosisResult(
        suggestedServices=[],
        suggestedParts=[
            SuggestedPartOut(partId="part-man-hinh-ip13", partName="Màn hình iPhone 13")
        ],
    )

    result = await QuotationAgent().run(state)
    quotation = result["quotation_result"]

    assert quotation.items == []
    assert any("hết hàng" in note for note in quotation.assumptions)


# ── B6: thiếu context -> NO_MATCH, không bịa -------------------------------------


@pytest.mark.asyncio
async def test_b6_missing_context_never_invents_data(monkeypatch):
    monkeypatch.setattr(settings, "llm_mode", "offline")

    response = await run_agents(
        _request(
            "Máy tôi bị hỏng",
            services=[],
            parts=[],
            slots=[],
            allow_quotation=False,
            allow_appointment=False,
        )
    )

    assert response.status is AgentStatus.NO_MATCH
    assert response.diagnosis is None or not response.diagnosis.suggested_services


# ── B7: AI không được tạo lịch hẹn / trả trạng thái xác nhận ---------------------


def test_b7_response_schema_rejects_business_action_fields():
    base = {
        "requestId": "b24f61c0-3f75-4c1e-b19e-4d5d2c7f22e1",
        "status": "SUCCESS",
        "intent": "UNKNOWN",
        "executedAgents": [],
    }
    with pytest.raises(ValidationError):
        AgentResponse.model_validate({**base, "appointmentId": "abc"})
    with pytest.raises(ValidationError):
        AgentResponse.model_validate({**base, "confirmed": True})


@pytest.mark.asyncio
async def test_b7_fabricated_slot_is_dropped_by_validation():
    state = _initial_state(_request("Mai 3h chiều", allow_quotation=False))
    state["plan"] = ["APPOINTMENT"]
    state["executed_agents"] = ["APPOINTMENT"]
    state["appointment_result"] = AppointmentResult(
        suggestedSlots=[
            SuggestedSlot(
                timeSlotId="slot-fake",
                slotStart="09:00",
                slotEnd="10:00",
                available=5,
                isFull=False,
                appointmentDate="2026-11-10",
            )
        ],
        missingInformation=[],
        explanation="Slot bịa.",
    )

    result = await validate_node(state)
    response = result["final_response"]

    assert response.appointment.suggested_slots == []
    assert response.status is AgentStatus.NO_MATCH


# ── B8: giá AI trả khác context bị ghi đè ----------------------------------------


@pytest.mark.asyncio
async def test_b8_quotation_price_is_overwritten_from_context():
    state = _initial_state(_request("Pin tụt nhanh", allow_quotation=False))
    state["plan"] = ["DIAGNOSIS"]
    state["executed_agents"] = ["DIAGNOSIS"]
    state["diagnosis_result"] = DiagnosisResult(
        suggestedServices=[
            SuggestedServiceOut(serviceId="svc-pin", serviceName="Thay pin")
        ]
    )
    state["quotation_result"] = QuotationResult(
        items=[
            QuotationItem(
                itemId="svc-pin",
                itemType="SERVICE",
                name="Thay pin",
                quantity=1,
                unitPrice=1,
                lineTotal=1,
            )
        ],
        estimatedTotal=1,
    )

    result = await validate_node(state)
    quotation = result["final_response"].quotation

    assert quotation is not None
    assert quotation.items[0].unit_price == 400000
    assert quotation.estimated_total == 400000


# ── Bổ sung: slot hợp lệ nhưng available=0 theo context --------------------------


@pytest.mark.asyncio
async def test_slot_full_in_context_is_dropped_even_if_ai_says_available():
    slots = [dict(CORE_SLOTS[2], available=0, isFull=True)]
    state = _initial_state(
        _request("Mai 3h chiều", slots=slots, allow_quotation=False)
    )
    state["plan"] = ["APPOINTMENT"]
    state["executed_agents"] = ["APPOINTMENT"]
    state["appointment_result"] = AppointmentResult(
        suggestedSlots=[
            SuggestedSlot(
                timeSlotId="slot-1500",
                slotStart="15:00",
                slotEnd="16:00",
                available=3,
                isFull=False,
                appointmentDate="2026-11-10",
            )
        ],
        missingInformation=[],
        explanation="AI nói còn chỗ.",
    )

    result = await validate_node(state)

    assert result["final_response"].appointment.suggested_slots == []
