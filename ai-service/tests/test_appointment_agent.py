"""Task 3.2 — Appointment Agent: đề xuất slot, bookingDraft, không tạo lịch hẹn."""

from __future__ import annotations

from datetime import date

import pytest

from app.agents.appointment.agent import AppointmentAgent
from app.orchestration.graph import _initial_state
from app.schemas.agent_models import AgentRequest
from tests.fixtures.context import CORE_SLOTS, build_agent_payload

TODAY = date(2026, 11, 9)


def agent() -> AppointmentAgent:
    return AppointmentAgent(today_provider=lambda: TODAY)


def _state(message: str, **kwargs):
    payload = build_agent_payload(message, **kwargs)
    return _initial_state(AgentRequest.model_validate(payload))


@pytest.mark.asyncio
async def test_afternoon_request_matches_available_slot():
    state = _state("Mai 3h chiều còn chỗ không")

    result = await agent().run(state)
    appointment = result["appointment_result"]

    assert [slot.time_slot_id for slot in appointment.suggested_slots] == ["slot-1500"]
    assert appointment.booking_draft.appointment_date == "2026-11-10"
    assert appointment.booking_draft.time_slot_id == "slot-1500"
    assert appointment.requires_user_confirmation is True


@pytest.mark.asyncio
async def test_full_slot_is_not_suggested_and_nearest_is_used():
    state = _state("Mai 14h còn chỗ không")

    result = await agent().run(state)
    appointment = result["appointment_result"]

    ids = [slot.time_slot_id for slot in appointment.suggested_slots]
    assert "slot-1400" not in ids  # slot đầy
    assert ids[0] == "slot-1500"  # gần 14:00 nhất
    assert appointment.booking_draft.time_slot_id is None
    assert "timeSlotId" in appointment.missing_information


@pytest.mark.asyncio
async def test_missing_customer_information_is_reported():
    state = _state("Mai 3h chiều")

    result = await agent().run(state)
    appointment = result["appointment_result"]

    assert "fullName" in appointment.missing_information
    assert "phone" in appointment.missing_information


@pytest.mark.asyncio
async def test_customer_information_fills_booking_draft():
    state = _state(
        "Mai 3h chiều",
        customer_info={"fullName": "Nguyễn Văn A", "phone": "0901 234 567"},
    )

    result = await agent().run(state)
    draft = result["appointment_result"].booking_draft

    assert draft.full_name == "Nguyễn Văn A"
    assert draft.phone == "0901234567"
    assert draft.time_slot_id == "slot-1500"


@pytest.mark.asyncio
async def test_date_without_slots_falls_back_to_earliest_available_date():
    slots = [
        dict(CORE_SLOTS[0], appointmentDate="2026-11-12"),
        dict(CORE_SLOTS[2], appointmentDate="2026-11-13"),
    ]
    state = _state("Mai 3h chiều", slots=slots)

    result = await agent().run(state)
    appointment = result["appointment_result"]

    assert appointment.booking_draft.appointment_date == "2026-11-12"
    assert appointment.suggested_slots
    assert "không còn khung trống" in appointment.explanation


@pytest.mark.asyncio
async def test_no_slots_from_backend_returns_guidance():
    state = _state("Mai 3h chiều", slots=[])

    result = await agent().run(state)
    appointment = result["appointment_result"]

    assert appointment.suggested_slots == []
    assert "khung giờ còn trống" in appointment.missing_information
    assert appointment.booking_draft.time_slot_id is None


@pytest.mark.asyncio
async def test_past_date_is_rejected_softly():
    state = _state("Hôm qua còn chỗ không")

    result = await agent().run(state)
    appointment = result["appointment_result"]

    assert appointment.suggested_slots == []
    assert "appointmentDate" in appointment.missing_information
    assert "đã qua" in appointment.explanation


@pytest.mark.asyncio
async def test_all_slots_full_returns_no_suggestion():
    slots = [
        dict(CORE_SLOTS[0], available=0, isFull=True),
        dict(CORE_SLOTS[1], available=0, isFull=True),
        dict(CORE_SLOTS[2], available=0, isFull=True),
    ]
    state = _state("Mai còn chỗ không", slots=slots)

    result = await agent().run(state)
    appointment = result["appointment_result"]

    assert appointment.suggested_slots == []


@pytest.mark.asyncio
async def test_agent_never_returns_appointment_id_or_confirmed_status():
    state = _state("Mai 3h chiều")

    result = await agent().run(state)
    appointment = result["appointment_result"]
    payload = appointment.model_dump(by_alias=True)

    assert "appointmentId" not in payload
    assert "status" not in payload
    assert payload["requiresUserConfirmation"] is True
    assert payload["bookingDraft"]["timeSlotId"] in {slot["timeSlotId"] for slot in CORE_SLOTS}


@pytest.mark.asyncio
async def test_phone_with_invalid_length_is_treated_as_missing():
    state = _state("Mai 3h chiều", customer_info={"fullName": "A", "phone": "123"})

    result = await agent().run(state)
    draft = result["appointment_result"].booking_draft

    assert draft.phone is None
    assert "phone" in result["appointment_result"].missing_information
