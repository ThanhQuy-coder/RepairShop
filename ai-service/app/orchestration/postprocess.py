"""
Tổng hợp & hậu kiểm kết quả MAS (Task 2.1, cập nhật Task 3.3).

- `aggregate_node`: tính status + reason từ kết quả/lỗi của các agent.
- `validate_node`: chốt lần cuối (ID/giá/slot phải khớp context) rồi dựng `AgentResponse`.
  Đây là lớp "zero-trust" phía AI; Backend vẫn hậu kiểm lần nữa.

Quy ước status (HUONG_DAN_TRIEN_KHAI_MAS_4_NGAY.md mục 4.6):
SUCCESS | PARTIAL | NO_MATCH | OUT_OF_SCOPE | ERROR
"""

from __future__ import annotations

import logging
from typing import Dict, Optional

from app.agents.appointment.agent import appointment_summary
from app.agents.quotation.agent import quote_summary
from app.core.config import settings
from app.schemas.agent_models import (
    DEFAULT_MAS_DISCLAIMER,
    AgentMeta,
    AgentName,
    AgentResponse,
    AgentStatus,
    AppointmentResult,
    DiagnosisResult,
    Intent,
    QuotationResult,
)
from app.schemas.state_models import AgentState

logger = logging.getLogger(__name__)

UNKNOWN_REASON = (
    "Tôi chưa xác định được yêu cầu. Bạn có thể mô tả tình trạng thiết bị "
    "(ví dụ: 'iPhone 13 pin tụt nhanh'), hỏi giá dịch vụ, hoặc yêu cầu đặt lịch."
)
TRACKING_REASON = (
    "Bản này chưa hỗ trợ tra cứu tiến độ qua trợ lý. "
    "Vui lòng dùng trang tra cứu phiếu sửa chữa bằng mã phiếu trên website."
)


def _diagnosis_has_result(result: Optional[DiagnosisResult]) -> bool:
    return result is not None and bool(result.suggested_services or result.suggested_parts)


def _quotation_has_result(result: Optional[QuotationResult]) -> bool:
    return result is not None and bool(result.items)


def _appointment_has_result(result: Optional[AppointmentResult]) -> bool:
    return result is not None and bool(result.suggested_slots)


def _has_meaningful_result(state: AgentState) -> bool:
    return (
        _diagnosis_has_result(state.get("diagnosis_result"))
        or _quotation_has_result(state.get("quotation_result"))
        or _appointment_has_result(state.get("appointment_result"))
    )


def compute_status(state: AgentState) -> AgentStatus:
    planned = state.get("plan") or []
    errors = state.get("errors") or []

    if state.get("out_of_scope"):
        return AgentStatus.OUT_OF_SCOPE

    if not planned:
        # UNKNOWN / TRACKING: không gọi agent — trả lời hướng dẫn, không phải lỗi.
        return AgentStatus.SUCCESS

    meaningful = _has_meaningful_result(state)

    if errors:
        return AgentStatus.PARTIAL if meaningful else AgentStatus.ERROR

    if not meaningful:
        return AgentStatus.NO_MATCH

    return AgentStatus.SUCCESS


def build_reason(status: AgentStatus, state: AgentState) -> str:
    diagnosis = state.get("diagnosis_result")
    quotation = state.get("quotation_result")
    appointment = state.get("appointment_result")
    errors = state.get("errors") or []
    intent = state.get("intent", Intent.UNKNOWN.value)

    if status is AgentStatus.OUT_OF_SCOPE:
        if diagnosis and diagnosis.explanation:
            return diagnosis.explanation[:500]
        return "Yêu cầu nằm ngoài phạm vi tư vấn sửa chữa thiết bị."

    if not state.get("plan"):
        return TRACKING_REASON if intent == Intent.TRACKING.value else UNKNOWN_REASON

    if status is AgentStatus.ERROR:
        note = errors[0].message if errors else ""
        return (
            "Không thể xử lý yêu cầu tư vấn lúc này. Vui lòng thử lại sau."
            if not note
            else f"Không thể hoàn tất yêu cầu: {note}"[:500]
        )

    if status is AgentStatus.NO_MATCH:
        if diagnosis and diagnosis.explanation:
            return diagnosis.explanation[:500]
        if appointment and appointment.explanation:
            return appointment.explanation[:500]
        return (
            "Chưa tìm thấy dịch vụ hoặc khung giờ phù hợp. "
            "Vui lòng mô tả rõ hơn hoặc chọn ngày khác."
        )

    parts = []
    if diagnosis and diagnosis.explanation:
        parts.append(diagnosis.explanation)
    quote_text = quote_summary(quotation)
    if quote_text:
        parts.append(quote_text)
    appointment_text = appointment_summary(appointment)
    if appointment_text:
        parts.append(appointment_text)
    if status is AgentStatus.PARTIAL and errors:
        parts.append(errors[0].message)

    reason = " ".join(part for part in parts if part).strip()
    if not reason:
        reason = "Đã xử lý yêu cầu."
    return reason[:500]


async def aggregate_node(state: AgentState) -> Dict[str, object]:
    if state.get("final_response"):
        return {}
    status = compute_status(state)
    return {"final_status": status.value, "final_reason": build_reason(status, state)}


def _clean_diagnosis(
    diagnosis: Optional[DiagnosisResult], state: AgentState
) -> Optional[DiagnosisResult]:
    """Loại lần cuối mọi ID không thuộc context (phòng thủ; agent đã lọc trước đó)."""
    if diagnosis is None:
        return None

    service_ids = {item["serviceId"] for item in state.get("available_services") or []}
    part_ids = {item["partId"] for item in state.get("available_parts") or []}

    kept_services = []
    seen_services: set[str] = set()
    for item in diagnosis.suggested_services:
        if item.service_id in service_ids and item.service_id not in seen_services:
            seen_services.add(item.service_id)
            kept_services.append(item)

    kept_parts = []
    seen_parts: set[str] = set()
    for item in diagnosis.suggested_parts:
        if item.part_id in part_ids and item.part_id not in seen_parts:
            seen_parts.add(item.part_id)
            kept_parts.append(item)

    if len(kept_services) != len(diagnosis.suggested_services):
        logger.warning(
            "validate: loại %d serviceId không thuộc context.",
            len(diagnosis.suggested_services) - len(kept_services),
        )
    if len(kept_parts) != len(diagnosis.suggested_parts):
        logger.warning(
            "validate: loại %d partId không thuộc context.",
            len(diagnosis.suggested_parts) - len(kept_parts),
        )

    if len(kept_services) == len(diagnosis.suggested_services) and len(kept_parts) == len(
        diagnosis.suggested_parts
    ):
        return diagnosis

    return diagnosis.model_copy(
        update={"suggested_services": kept_services, "suggested_parts": kept_parts}
    )


def _clean_quotation(
    quotation: Optional[QuotationResult], state: AgentState
) -> Optional[QuotationResult]:
    """Ghi đè giá bằng giá context và tính lại tổng — Backend là nguồn giá duy nhất."""
    if quotation is None:
        return None

    price_by_item: Dict[str, float] = {}
    for item in state.get("available_services") or []:
        price_by_item[item["serviceId"]] = float(item["basePrice"])
    for item in state.get("available_parts") or []:
        price_by_item[item["partId"]] = float(item["unitPrice"])

    items = []
    seen_item_ids: set[str] = set()
    for item in quotation.items:
        if item.item_id in seen_item_ids:
            logger.warning(
                "validate: quotation item %s bị lặp — chỉ giữ 1 dòng.", item.item_id
            )
            continue
        context_price = price_by_item.get(item.item_id)
        if context_price is None:
            logger.warning(
                "validate: quotation item %s không thuộc context — loại bỏ.", item.item_id
            )
            continue
        seen_item_ids.add(item.item_id)
        items.append(
            item.model_copy(
                update={
                    "unit_price": context_price,
                    "line_total": round(context_price * item.quantity, 2),
                }
            )
        )

    total = round(sum(item.line_total for item in items), 2)
    if total != quotation.estimated_total:
        logger.warning(
            "validate: estimatedTotal %.0f không khớp context — tính lại %.0f.",
            quotation.estimated_total,
            total,
        )

    return quotation.model_copy(update={"items": items, "estimated_total": total})


def _clean_appointment(
    appointment: Optional[AppointmentResult], state: AgentState
) -> Optional[AppointmentResult]:
    """Chỉ giữ slot thuộc context, còn chỗ thật; xoá timeSlotId nếu không hợp lệ."""
    if appointment is None:
        return None

    context_slots = {
        item["timeSlotId"]: item for item in state.get("available_slots") or []
    }

    kept = []
    for slot in appointment.suggested_slots:
        context_slot = context_slots.get(slot.time_slot_id)
        if context_slot is None:
            logger.warning(
                "validate: slot %s không thuộc context — loại bỏ.", slot.time_slot_id
            )
            continue
        available = int(context_slot.get("available", 0))
        if available <= 0:
            logger.warning(
                "validate: slot %s đã đầy theo context — loại bỏ.", slot.time_slot_id
            )
            continue
        kept.append(
            slot.model_copy(
                update={
                    "available": available,
                    "is_full": available == 0,
                    "appointment_date": context_slot.get("appointmentDate"),
                }
            )
        )

    draft = appointment.booking_draft
    if draft is not None and draft.time_slot_id is not None:
        context_slot = context_slots.get(draft.time_slot_id)
        slot_date = context_slot.get("appointmentDate") if context_slot else None
        if (
            context_slot is None
            or int(context_slot.get("available", 0)) <= 0
            or (draft.appointment_date and slot_date and draft.appointment_date != slot_date)
        ):
            logger.warning(
                "validate: bookingDraft.timeSlotId %s không hợp lệ — xoá.", draft.time_slot_id
            )
            draft = draft.model_copy(update={"timeSlotId": None})

    missing = list(appointment.missing_information)
    if draft is not None and draft.time_slot_id is None and "timeSlotId" not in missing:
        missing.append("timeSlotId")

    return appointment.model_copy(
        update={
            "suggested_slots": kept,
            "booking_draft": draft,
            "missing_information": missing,
        }
    )


async def validate_node(state: AgentState) -> Dict[str, object]:
    diagnosis = _clean_diagnosis(state.get("diagnosis_result"), state)
    quotation = _clean_quotation(state.get("quotation_result"), state)
    appointment = _clean_appointment(state.get("appointment_result"), state)

    cleaned_state: AgentState = dict(state)
    cleaned_state["diagnosis_result"] = diagnosis
    cleaned_state["quotation_result"] = quotation
    cleaned_state["appointment_result"] = appointment

    status = compute_status(cleaned_state)
    reason = build_reason(status, cleaned_state)

    try:
        intent = Intent(state.get("intent", Intent.UNKNOWN.value))
    except ValueError:
        intent = Intent.UNKNOWN

    executed = [
        AgentName(value)
        for value in (state.get("executed_agents") or [])
        if value in {agent.value for agent in AgentName}
    ]

    response = AgentResponse(
        requestId=state["request_id"],
        status=status,
        intent=intent,
        executedAgents=executed,
        diagnosis=diagnosis,
        quotation=quotation,
        appointment=appointment,
        reason=reason,
        disclaimer=DEFAULT_MAS_DISCLAIMER,
        meta=AgentMeta(
            llmMode=(settings.llm_mode or "ollama").lower(),
            llmCalls=int(state.get("llm_calls") or 0),
            agentTimingsMs=dict(state.get("timings") or {}),
            model=settings.llm_model or None,
            fallbackUsed=bool(state.get("fallback_used")),
        ),
    )
    return {"final_response": response}
