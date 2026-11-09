"""Task 2.1 — Coordinator: intent + kế hoạch agent."""

from __future__ import annotations

import pytest

from app.agents.coordinator.agent import coordinator_node
from app.ml.intent_classifier import classify_intent
from app.orchestration.graph import _initial_state
from app.schemas.agent_models import AgentRequest, Intent


def _state(message: str, **options):
    payload = {
        "requestId": "b24f61c0-3f75-4c1e-b19e-4d5d2c7f22e1",
        "actorType": "GUEST",
        "message": message,
        "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"},
        "issueDescription": "",
        "context": {"availableServices": [], "availableParts": [], "availableSlots": []},
        "options": {
            "allowDiagnosis": options.get("allow_diagnosis", True),
            "allowQuotation": options.get("allow_quotation", True),
            "allowAppointment": options.get("allow_appointment", True),
        },
    }
    return _initial_state(AgentRequest.model_validate(payload))


@pytest.mark.asyncio
@pytest.mark.parametrize(
    "message,expected_intent",
    [
        ("iPhone 13 pin tụt nhanh, máy nóng khi sạc", "DIAGNOSIS"),
        ("Máy hiện lỗi error code 404 khi mở ứng dụng", "DIAGNOSIS"),
        ("Bàn phím không gõ được nút thanh toán trên màn hình", "DIAGNOSIS"),
        ("Máy đọc mã QR code bị mờ", "DIAGNOSIS"),
        ("Điện thoại bị vỡ màn hình", "DIAGNOSIS"),
        ("Cho tôi hỏi giá thay pin iPhone 13 là bao nhiêu", "QUOTATION"),
        ("Báo giá giúp tôi dịch vụ sửa chữa", "QUOTATION"),
        ("Mai 3h chiều còn chỗ không, tôi muốn đặt lịch", "APPOINTMENT"),
        ("Chiều nay còn khung giờ nào trống không", "APPOINTMENT"),
        ("Tra cứu giúp tôi mã đơn RS-123", "TRACKING"),
        ("Tiến độ sửa máy của tôi tới đâu rồi", "TRACKING"),
        ("Kể cho tôi một câu chuyện cười", "UNKNOWN"),
        ("Xin chào", "UNKNOWN"),
        ("Viết giúp tôi một bài thơ", "UNKNOWN"),
    ],
)
async def test_coordinator_classifies_intent(message, expected_intent):
    result = await coordinator_node(_state(message))
    assert result["intent"] == expected_intent
    assert 0.0 <= result["intent_confidence"] <= 1.0


@pytest.mark.asyncio
async def test_coordinator_builds_full_plan_for_diagnosis_when_all_agents_allowed():
    result = await coordinator_node(_state("Pin tụt nhanh, máy nóng khi sạc"))
    assert result["plan"] == ["DIAGNOSIS", "QUOTATION", "APPOINTMENT"]


@pytest.mark.asyncio
async def test_coordinator_only_adds_allowed_agents():
    result = await coordinator_node(
        _state("Pin tụt nhanh", allow_quotation=False, allow_appointment=False)
    )
    assert result["plan"] == ["DIAGNOSIS"]


@pytest.mark.asyncio
async def test_coordinator_never_adds_unallowed_quotation():
    result = await coordinator_node(
        _state("Cho hỏi giá thay pin", allow_quotation=False)
    )
    # QUOTATION bị tắt => fallback agent được phép (DIAGNOSIS), không chạy Quotation.
    assert result["plan"] == ["DIAGNOSIS"]


@pytest.mark.asyncio
async def test_mixed_fault_price_booking_message_is_still_diagnosis_first():
    """Câu vừa tả lỗi vừa hỏi giá vừa đặt lịch: phải chẩn đoán trước, rồi mới báo giá/đặt lịch."""
    result = await coordinator_node(
        _state(
            "Pin tụt nhanh, máy nóng khi sạc. Muốn báo giá và đặt lịch mai 3h chiều."
        )
    )

    assert result["intent"] == "DIAGNOSIS"
    assert result["plan"] == ["DIAGNOSIS", "QUOTATION", "APPOINTMENT"]


@pytest.mark.asyncio
async def test_unknown_intent_plan_is_empty_and_reason_will_ask_for_clarification():
    result = await coordinator_node(_state("Bạn rảnh không"))
    assert result["intent"] == Intent.UNKNOWN.value
    assert result["plan"] == []


def test_classifier_confidence_is_bounded_and_source_is_known():
    signals = classify_intent("Pin tụt nhanh, máy nóng")
    assert signals.intent is Intent.DIAGNOSIS
    assert 0.0 <= signals.confidence <= 1.0
    assert signals.source in {"rule", "ml"}


def test_classifier_uses_rule_for_strong_signals():
    signals = classify_intent("Máy hiện lỗi error code 404 khi mở ứng dụng")
    assert signals.intent is Intent.DIAGNOSIS
    assert signals.source == "rule"
