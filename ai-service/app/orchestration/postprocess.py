"""
Tổng hợp & hậu kiểm kết quả MAS (Task 2.1, cập nhật Task 3.3 & TASK-10/11/13).

- `aggregate_node`: tính status + reason từ kết quả/lỗi của các agent.
- `validate_node`: chốt lần cuối (ID/giá/slot/warranty/tracking phải khớp context) rồi dựng `AgentResponse`.
  Đây là lớp "zero-trust" phía AI; Backend vẫn hậu kiểm lần nữa.

Quy ước status chuẩn hoá (TASK-02):
SUCCESS | PARTIAL | NEED_CLARIFICATION | NO_MATCH | OUT_OF_SCOPE | ERROR
"""

from __future__ import annotations

import logging
from typing import Dict, List, Optional

from app.agents.appointment.agent import appointment_summary
from app.agents.quotation.agent import quote_summary
from app.agents.tracking.agent import tracking_summary
from app.agents.warranty.agent import warranty_summary
from app.core.config import settings
from app.schemas.agent_models import (
    DEFAULT_MAS_DISCLAIMER,
    AgentMeta,
    AgentName,
    AgentResponse,
    AgentStatus,
    AppointmentResult,
    DiagnosisResult,
    IdentifiedEntities,
    Intent,
    MasTrackingLookup,
    MasWarrantyPolicy,
    MasWarrantyTicketLookup,
    QuotationResult,
    TrackingHistoryItemOut,
    TrackingResult,
    TrackingTicketOut,
    WarrantyLookupOut,
    WarrantyPackageOut,
    WarrantyResult,
)
from app.schemas.state_models import AgentState

logger = logging.getLogger(__name__)

UNKNOWN_REASON = (
    "Tôi chưa xác định được yêu cầu. Bạn có thể mô tả tình trạng thiết bị "
    "(ví dụ: 'iPhone 13 pin tụt nhanh'), hỏi giá dịch vụ, kiểm tra bảo hành hoặc đặt lịch."
)
TRACKING_REASON = (
    "Bản này chưa bật tra cứu tiến độ trong context hiện tại. "
    "Vui lòng dùng trang tra cứu phiếu sửa chữa bằng mã phiếu trên website."
)
CLARIFICATION_REASON = (
    "Vui lòng cung cấp thêm thông tin cụ thể (triệu chứng, mã phiếu sửa chữa hoặc mã bảo hành) "
    "để hệ thống tư vấn chính xác hơn."
)


def _diagnosis_has_result(result: Optional[DiagnosisResult]) -> bool:
    return result is not None and bool(result.suggested_services or result.suggested_parts)


def _quotation_has_result(result: Optional[QuotationResult]) -> bool:
    return result is not None and bool(result.items)


def _appointment_has_result(result: Optional[AppointmentResult]) -> bool:
    return result is not None and bool(result.suggested_slots)


def _warranty_has_result(result: Optional[WarrantyResult]) -> bool:
    return result is not None and bool(result.policies or result.warranties)


def _tracking_has_result(result: Optional[TrackingResult]) -> bool:
    return result is not None and bool(result.tickets)


def _has_meaningful_result(state: AgentState) -> bool:
    return (
        _diagnosis_has_result(state.get("diagnosis_result"))
        or _quotation_has_result(state.get("quotation_result"))
        or _appointment_has_result(state.get("appointment_result"))
        or _warranty_has_result(state.get("warranty_result"))
        or _tracking_has_result(state.get("tracking_result"))
    )


def compute_status(state: AgentState) -> AgentStatus:
    planned = state.get("plan") or []
    errors = state.get("errors") or []

    if state.get("out_of_scope"):
        return AgentStatus.OUT_OF_SCOPE

    if not planned:
        # UNKNOWN hoặc feature chưa bật trong options: trả lời hướng dẫn.
        return AgentStatus.SUCCESS

    meaningful = _has_meaningful_result(state)

    if errors:
        return AgentStatus.PARTIAL if meaningful else AgentStatus.ERROR

    if not meaningful and state.get("need_clarification"):
        return AgentStatus.NEED_CLARIFICATION

    if not meaningful:
        return AgentStatus.NO_MATCH

    return AgentStatus.SUCCESS


def build_reason(status: AgentStatus, state: AgentState) -> str:
    diagnosis = state.get("diagnosis_result")
    quotation = state.get("quotation_result")
    appointment = state.get("appointment_result")
    warranty = state.get("warranty_result")
    tracking = state.get("tracking_result")
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

    if status is AgentStatus.NEED_CLARIFICATION:
        for res in (warranty, tracking, diagnosis, appointment):
            if res and res.explanation:
                return res.explanation[:500]
        if quotation and quotation.explanation:
            return quotation.explanation[:500]
        return CLARIFICATION_REASON

    if status is AgentStatus.NO_MATCH:
        if diagnosis and diagnosis.explanation:
            return diagnosis.explanation[:500]
        if warranty and warranty.explanation:
            return warranty.explanation[:500]
        if tracking and tracking.explanation:
            return tracking.explanation[:500]
        if appointment and appointment.explanation:
            return appointment.explanation[:500]
        if quotation and quotation.explanation:
            return quotation.explanation[:500]
        return (
            "Chưa tìm thấy dịch vụ, khung giờ hoặc hồ sơ phù hợp. "
            "Vui lòng mô tả rõ hơn hoặc kiểm tra lại mã tra cứu."
        )

    parts = []
    if warranty and warranty_summary(warranty):
        parts.append(warranty_summary(warranty))
    if tracking and tracking_summary(tracking):
        parts.append(tracking_summary(tracking))
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
            draft = draft.model_copy(update={"time_slot_id": None})

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


def _clean_warranty(
    warranty: Optional[WarrantyResult], state: AgentState
) -> Optional[WarrantyResult]:
    """Chỉ giữ gói bảo hành & phiếu bảo hành có thật trong context Backend."""
    if warranty is None:
        return None

    policy_by_id: Dict[str, MasWarrantyPolicy] = {}
    for raw in state.get("warranty_policies") or []:
        p = MasWarrantyPolicy.model_validate(raw)
        policy_by_id[p.package_id] = p

    lookup_by_code: Dict[str, MasWarrantyTicketLookup] = {}
    for raw in state.get("warranty_lookups") or []:
        w = MasWarrantyTicketLookup.model_validate(raw)
        lookup_by_code[w.warranty_code.upper()] = w

    kept_policies: List[WarrantyPackageOut] = []
    seen_policies: set[str] = set()
    for p_out in warranty.policies:
        ctx = policy_by_id.get(p_out.package_id)
        if ctx is None or ctx.package_id in seen_policies:
            logger.warning("validate: packageId %s không thuộc context — loại.", p_out.package_id)
            continue
        seen_policies.add(ctx.package_id)
        kept_policies.append(
            WarrantyPackageOut(
                packageId=ctx.package_id,
                name=ctx.name,
                durationMonths=ctx.duration_months,
                terms=ctx.terms,
                description=ctx.description,
            )
        )

    kept_warranties: List[WarrantyLookupOut] = []
    seen_warranties: set[str] = set()
    for w_out in warranty.warranties:
        ctx = lookup_by_code.get(w_out.warranty_code.upper())
        if ctx is None or ctx.warranty_code.upper() in seen_warranties:
            logger.warning(
                "validate: warrantyCode %s không thuộc context — loại.", w_out.warranty_code
            )
            continue
        seen_warranties.add(ctx.warranty_code.upper())
        kept_warranties.append(
            WarrantyLookupOut(
                warrantyCode=ctx.warranty_code,
                ticketCode=ctx.ticket_code,
                deviceLabel=ctx.device_label,
                status=w_out.status if ctx.status != "Voided" else "Voided",
                startDate=ctx.start_date,
                endDate=ctx.end_date,
                isExpired=bool(ctx.is_expired or w_out.is_expired),
                packageName=ctx.package_name,
                terms=ctx.terms,
            )
        )

    return warranty.model_copy(
        update={
            "policies": kept_policies,
            "warranties": kept_warranties,
            "requires_verification": bool(kept_warranties or warranty.requires_verification),
        }
    )


def _clean_tracking(
    tracking: Optional[TrackingResult], state: AgentState
) -> Optional[TrackingResult]:
    """Chỉ giữ phiếu sửa chữa thuộc context Backend và đồng bộ trạng thái thật."""
    if tracking is None:
        return None

    lookup_by_code: Dict[str, MasTrackingLookup] = {}
    for raw in state.get("tracking_lookups") or []:
        t = MasTrackingLookup.model_validate(raw)
        lookup_by_code[t.ticket_code.upper()] = t

    kept_tickets: List[TrackingTicketOut] = []
    seen_codes: set[str] = set()
    for t_out in tracking.tickets:
        ctx = lookup_by_code.get(t_out.ticket_code.upper())
        if ctx is None or ctx.ticket_code.upper() in seen_codes:
            logger.warning(
                "validate: ticketCode %s không thuộc context — loại.", t_out.ticket_code
            )
            continue
        seen_codes.add(ctx.ticket_code.upper())
        kept_tickets.append(
            TrackingTicketOut(
                ticketCode=ctx.ticket_code,
                deviceLabel=ctx.device_label,
                status=ctx.status,
                statusLabel=ctx.status_label,
                statusHistory=[
                    TrackingHistoryItemOut(
                        status=h.status,
                        statusLabel=h.status_label,
                        changedAt=h.changed_at,
                    )
                    for h in ctx.status_history
                ],
                estimatedCompletion=ctx.estimated_completion,
            )
        )

    return tracking.model_copy(update={"tickets": kept_tickets})


def _collect_missing_information(
    state: AgentState,
    diagnosis: Optional[DiagnosisResult],
    appointment: Optional[AppointmentResult],
    warranty: Optional[WarrantyResult],
    tracking: Optional[TrackingResult],
) -> List[str]:
    merged: List[str] = []
    for source in (
        state.get("missing_information") or [],
        diagnosis.missing_information if diagnosis else [],
        appointment.missing_information if appointment else [],
        warranty.missing_information if warranty else [],
        tracking.missing_information if tracking else [],
    ):
        for item in source:
            cleaned = (item or "").strip()
            if cleaned and cleaned not in merged:
                merged.append(cleaned)
    return merged[:10]


async def validate_node(state: AgentState) -> Dict[str, object]:
    diagnosis = _clean_diagnosis(state.get("diagnosis_result"), state)
    quotation = _clean_quotation(state.get("quotation_result"), state)
    appointment = _clean_appointment(state.get("appointment_result"), state)
    warranty = _clean_warranty(state.get("warranty_result"), state)
    tracking = _clean_tracking(state.get("tracking_result"), state)

    cleaned_state: AgentState = dict(state)
    cleaned_state["diagnosis_result"] = diagnosis
    cleaned_state["quotation_result"] = quotation
    cleaned_state["appointment_result"] = appointment
    cleaned_state["warranty_result"] = warranty
    cleaned_state["tracking_result"] = tracking

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

    entities = state.get("identified_entities")
    if entities is not None and not isinstance(entities, IdentifiedEntities):
        entities = IdentifiedEntities.model_validate(entities)

    missing_information = _collect_missing_information(
        cleaned_state, diagnosis, appointment, warranty, tracking
    )

    response = AgentResponse(
        requestId=state["request_id"],
        status=status,
        intent=intent,
        executedAgents=executed,
        diagnosis=diagnosis,
        quotation=quotation,
        appointment=appointment,
        warranty=warranty,
        tracking=tracking,
        identifiedEntities=entities,
        missingInformation=missing_information,
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
