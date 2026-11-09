"""
Appointment Agent (Task 3.2).

Chức năng: đọc ngày/giờ người dùng muốn, đối chiếu `availableSlots` do Backend cung cấp,
đề xuất khung giờ còn chỗ, chuẩn bị `bookingDraft`.

Giới hạn bắt buộc:
- KHÔNG tạo slot, KHÔNG tạo lịch hẹn, KHÔNG trả `appointmentId`/`status=Confirmed`.
- Chỉ đề xuất slot nằm trong context; slot đầy (`available == 0`) bị loại.
- Luôn `requiresUserConfirmation = true` — Backend chỉ tạo lịch sau khi người dùng xác nhận.
"""

from __future__ import annotations

import logging
import time
from datetime import date
from typing import Callable, Dict, List, Optional

from app.agents.appointment.datetime_parser import (
    TimePreference,
    parse_requested_date,
    parse_requested_time,
)
from app.schemas.agent_models import (
    AgentError,
    AgentName,
    AppointmentResult,
    BookingDraft,
    MasSlot,
    SuggestedSlot,
)
from app.schemas.state_models import AgentState

logger = logging.getLogger(__name__)

MAX_SUGGESTED_SLOTS = 3


def _slots(state: AgentState) -> List[MasSlot]:
    return [MasSlot.model_validate(raw) for raw in state.get("available_slots") or []]


def _normalize_phone(phone: Optional[str]) -> Optional[str]:
    if not phone:
        return None
    digits = "".join(character for character in phone if character.isdigit())
    return digits if 9 <= len(digits) <= 11 else None


def _slot_matches_date(slot: MasSlot, target: date) -> bool:
    return slot.appointment_date == target.isoformat()


def _slot_contains(slot: MasSlot, preference: TimePreference) -> bool:
    return slot.slot_start <= preference.as_hhmm() < slot.slot_end


def _minutes(hhmm: str) -> int:
    hour, minute = hhmm.split(":")
    return int(hour) * 60 + int(minute)


def _distance(slot: MasSlot, preference: TimePreference) -> int:
    return abs(_minutes(slot.slot_start) - _minutes(preference.as_hhmm()))


def _to_suggested(slot: MasSlot) -> SuggestedSlot:
    return SuggestedSlot(
        timeSlotId=slot.time_slot_id,
        slotStart=slot.slot_start,
        slotEnd=slot.slot_end,
        available=slot.available,
        isFull=bool(slot.is_full),
        appointmentDate=slot.appointment_date,
    )


class AppointmentAgent:
    """`today_provider` cho phép test cố định ngày (mặc định: ngày thật)."""

    def __init__(self, today_provider: Optional[Callable[[], date]] = None) -> None:
        self._today = today_provider or date.today

    def _draft(
        self,
        state: AgentState,
        target_date: Optional[date],
        time_slot_id: Optional[str],
    ) -> BookingDraft:
        customer = state.get("customer_info") or {}
        device = state.get("device") or {}
        return BookingDraft(
            fullName=(customer.get("fullName") or "").strip()[:100] or None,
            phone=_normalize_phone(customer.get("phone")),
            appointmentDate=target_date.isoformat() if target_date else None,
            timeSlotId=time_slot_id,
            deviceType=device.get("deviceType"),
            brand=device.get("brand"),
            model=device.get("model"),
            issueDescription=(state.get("issue_description") or None),
            # Không có field nào cho status/appointmentId — AI không được tạo lịch hẹn.
        )

    def _customer_missing(self, draft: BookingDraft) -> List[str]:
        missing: List[str] = []
        if not draft.full_name:
            missing.append("fullName")
        if not draft.phone:
            missing.append("phone")
        return missing

    async def run(self, state: AgentState) -> Dict[str, object]:
        started = time.perf_counter()
        today = self._today()

        text = " ".join(
            part
            for part in (
                state.get("user_message") or "",
                state.get("issue_description") or "",
            )
            if part
        )
        requested_date = parse_requested_date(text, today)
        requested_time = parse_requested_time(text)

        slots = _slots(state)
        errors: List[object] = list(state.get("errors") or [])  # giữ nguyên lỗi agent trước

        result = self._build_result(
            state, slots, today, requested_date, requested_time
        )

        elapsed_ms = int((time.perf_counter() - started) * 1000)
        timings = dict(state.get("timings") or {})
        timings[AgentName.APPOINTMENT.value] = elapsed_ms
        executed = list(state.get("executed_agents") or [])
        if AgentName.APPOINTMENT.value not in executed:
            executed.append(AgentName.APPOINTMENT.value)

        logger.info(
            "appointment request_id=%s requested_date=%s requested_time=%s slots=%d missing=%s elapsed_ms=%d",
            state.get("request_id"),
            requested_date,
            requested_time.as_hhmm() if requested_time else None,
            len(result.suggested_slots),
            result.missing_information,
            elapsed_ms,
        )

        return {
            "appointment_result": result,
            "executed_agents": executed,
            "timings": timings,
            "errors": errors,
        }

    def _build_result(
        self,
        state: AgentState,
        slots: List[MasSlot],
        today: date,
        requested_date: Optional[date],
        requested_time: Optional[TimePreference],
    ) -> AppointmentResult:
        if not slots:
            draft = self._draft(state, None, None)
            return AppointmentResult(
                suggestedSlots=[],
                bookingDraft=draft,
                missingInformation=self._customer_missing(draft) + ["khung giờ còn trống"],
                requiresUserConfirmation=True,
                explanation=(
                    "Backend chưa cung cấp khung giờ trống cho ngày bạn muốn. "
                    "Vui lòng chọn ngày khác hoặc liên hệ cửa hàng."
                ),
            )

        if requested_date is not None and requested_date < today:
            draft = self._draft(state, None, None)
            return AppointmentResult(
                suggestedSlots=[],
                bookingDraft=draft,
                missingInformation=self._customer_missing(draft) + ["appointmentDate"],
                requiresUserConfirmation=True,
                explanation="Ngày bạn chọn đã qua. Vui lòng chọn ngày khác.",
            )

        available_slots = [slot for slot in slots if slot.available > 0 and not slot.is_full]

        target_date, note = self._resolve_target_date(
            available_slots, requested_date
        )
        candidates = [
            slot
            for slot in available_slots
            if target_date is not None and _slot_matches_date(slot, target_date)
        ]

        if not candidates:
            draft = self._draft(state, None, None)
            return AppointmentResult(
                suggestedSlots=[],
                bookingDraft=draft,
                missingInformation=self._customer_missing(draft) + ["khung giờ khác"],
                requiresUserConfirmation=True,
                explanation=self._no_slot_explanation(requested_date, note),
            )

        if requested_time is not None:
            exact = [slot for slot in candidates if _slot_contains(slot, requested_time)]
            if exact:
                matched = exact
                missing: List[str] = []
                unique_slot_id = matched[0].time_slot_id if len(matched) == 1 else None
                explanation = (
                    f"Khung {matched[0].slot_start}–{matched[0].slot_end} "
                    f"ngày {target_date.strftime('%d/%m/%Y')} còn chỗ."
                )
            else:
                matched = sorted(
                    candidates, key=lambda slot: _distance(slot, requested_time)
                )
                unique_slot_id = None
                missing = ["timeSlotId"]
                explanation = (
                    f"Khung {requested_time.as_hhmm()} không còn trống. "
                    f"Gợi ý khung gần nhất ngày {target_date.strftime('%d/%m/%Y')}."
                )
        else:
            matched = candidates
            unique_slot_id = None
            missing = ["timeSlotId"]
            explanation = (
                f"Ngày {target_date.strftime('%d/%m/%Y')} còn "
                f"{len(candidates)} khung giờ trống. Vui lòng chọn một khung."
            )

        suggested = [_to_suggested(slot) for slot in matched[:MAX_SUGGESTED_SLOTS]]
        if unique_slot_id is None and "timeSlotId" not in missing:
            missing.append("timeSlotId")
        draft = self._draft(state, target_date, unique_slot_id)

        missing_information = self._customer_missing(draft) + missing
        if note:
            explanation = f"{explanation} {note}".strip()

        return AppointmentResult(
            suggestedSlots=suggested,
            bookingDraft=draft,
            missingInformation=missing_information,
            requiresUserConfirmation=True,
            explanation=explanation[:600],
        )

    def _resolve_target_date(
        self, available_slots: List[MasSlot], requested_date: Optional[date]
    ) -> tuple[Optional[date], Optional[str]]:
        dated = [slot for slot in available_slots if slot.appointment_date]
        if not dated:
            return requested_date, None

        if requested_date is not None:
            if any(slot.appointment_date == requested_date.isoformat() for slot in dated):
                return requested_date, None
            earliest_text = min(slot.appointment_date for slot in dated if slot.appointment_date)
            note = (
                f"Ngày {requested_date.strftime('%d/%m/%Y')} không còn khung trống; "
                f"gợi ý ngày gần nhất {date.fromisoformat(earliest_text).strftime('%d/%m/%Y')}."
            )
            return date.fromisoformat(earliest_text), note

        earliest_text = min(slot.appointment_date for slot in dated if slot.appointment_date)
        return date.fromisoformat(earliest_text), None

    def _no_slot_explanation(
        self, requested_date: Optional[date], note: Optional[str]
    ) -> str:
        if requested_date is not None:
            message = (
                f"Không còn khung giờ trống cho ngày {requested_date.strftime('%d/%m/%Y')}."
            )
        else:
            message = "Hiện chưa có khung giờ trống phù hợp."
        return f"{message} Vui lòng chọn ngày khác. {note or ''}".strip()[:600]


_appointment_agent = AppointmentAgent()


async def appointment_node(state: AgentState) -> Dict[str, object]:
    """Node không được ném lỗi ra ngoài graph — lỗi cục bộ đi vào `errors[]`."""
    try:
        return await _appointment_agent.run(state)
    except Exception as exc:  # pragma: no cover - phòng lỗi bất ngờ
        logger.exception("Appointment agent lỗi: %s", exc)
        errors = list(state.get("errors") or [])
        errors.append(
            AgentError(
                agent=AgentName.APPOINTMENT.value,
                code="APPOINTMENT_FAILED",
                message="Không thể gợi ý khung giờ lúc này.",
            )
        )
        executed = list(state.get("executed_agents") or [])
        if AgentName.APPOINTMENT.value not in executed:
            executed.append(AgentName.APPOINTMENT.value)
        timings = dict(state.get("timings") or {})
        timings.setdefault(AgentName.APPOINTMENT.value, 0)
        return {"errors": errors, "executed_agents": executed, "timings": timings}


def appointment_summary(result: Optional[AppointmentResult]) -> str:
    """Câu tóm tắt dùng cho `reason`."""
    if result is None or not result.suggested_slots:
        return ""
    first = result.suggested_slots[0]
    date_text = first.appointment_date
    if date_text:
        try:
            date_text = date.fromisoformat(date_text).strftime("%d/%m/%Y")
        except ValueError:
            pass
    date_suffix = f" ngày {date_text}." if date_text else "."
    return (
        f"Còn {len(result.suggested_slots)} khung giờ trống, gần nhất "
        f"{first.slot_start}–{first.slot_end}{date_suffix}"
    )
