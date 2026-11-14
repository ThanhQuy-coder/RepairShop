"""
Tracking Agent — TASK-11.

Chức năng:
- Tra cứu tiến độ phiếu sửa chữa từ `context.trackingLookups` theo `ticketCode`
  hoặc lịch sử đơn công khai của khách hàng đã đăng nhập (`CUSTOMER`).
- Không lộ PII, không bịa tiến độ/ngày giao máy nếu Backend chưa cung cấp.
- Nếu thiếu `ticketCode` và không có đơn trong context -> `need_clarification=True`.
"""

from __future__ import annotations

import logging
import re
import time
from typing import Dict, List, Optional

from app.schemas.agent_models import (
    AgentError,
    AgentName,
    IdentifiedEntities,
    MasTrackingLookup,
    TrackingHistoryItemOut,
    TrackingResult,
    TrackingTicketOut,
)
from app.schemas.state_models import AgentState

logger = logging.getLogger(__name__)

TICKET_CODE_RE = re.compile(
    r"\b(?:RS|RT|TK|HD|SC)(?:[-_][A-Z0-9]{2,15}(?:[-_][A-Z0-9]{1,15})*|(?=[A-Z0-9]*\d)[A-Z0-9]{3,20})\b",
    re.IGNORECASE,
)


def extract_ticket_code(state: AgentState) -> Optional[str]:
    explicit = (state.get("ticket_code") or "").strip()
    if explicit:
        return explicit

    combined = " ".join(
        part
        for part in (
            state.get("user_message") or "",
            state.get("issue_description") or "",
        )
        if part
    )
    match = TICKET_CODE_RE.search(combined)
    return match.group(0).strip().upper() if match else None


def _to_ticket_out(lookup: MasTrackingLookup) -> TrackingTicketOut:
    return TrackingTicketOut(
        ticketCode=lookup.ticket_code,
        deviceLabel=lookup.device_label,
        status=lookup.status,
        statusLabel=lookup.status_label,
        statusHistory=[
            TrackingHistoryItemOut(
                status=item.status,
                statusLabel=item.status_label,
                changedAt=item.changed_at,
            )
            for item in lookup.status_history
        ],
        estimatedCompletion=lookup.estimated_completion,
    )


def _merge_entities(
    state: AgentState, *, ticket_code: Optional[str] = None
) -> IdentifiedEntities:
    existing = state.get("identified_entities")
    data = existing.model_dump() if isinstance(existing, IdentifiedEntities) else {}
    device = state.get("device") or {}
    data.setdefault("brand", device.get("brand"))
    data.setdefault("model", device.get("model"))
    data.setdefault("device_type", device.get("deviceType"))
    if ticket_code:
        data["ticket_code"] = ticket_code
    return IdentifiedEntities.model_validate(data)


class TrackingAgent:
    """Agent tra cứu trạng thái phiếu sửa chữa chỉ đọc dữ liệu từ context."""

    async def run(self, state: AgentState) -> Dict[str, object]:
        started = time.perf_counter()
        code = extract_ticket_code(state)
        lookups = [
            MasTrackingLookup.model_validate(item)
            for item in (state.get("tracking_lookups") or [])
        ]

        need_clarification = False
        missing_info: List[str] = list(state.get("missing_information") or [])
        matched_code: Optional[str] = code

        if code:
            matched = next(
                (item for item in lookups if item.ticket_code.upper() == code.upper()),
                None,
            )
            if matched is None:
                result = TrackingResult(
                    tickets=[],
                    missingInformation=[],
                    explanation=(
                        f"Không tìm thấy phiếu sửa chữa với mã {code}. "
                        "Vui lòng kiểm tra lại mã phiếu hoặc liên hệ cửa hàng."
                    ),
                )
            else:
                out = _to_ticket_out(matched)
                matched_code = out.ticket_code
                result = TrackingResult(
                    tickets=[out],
                    missingInformation=[],
                    explanation=self._describe_ticket(out),
                )
        elif lookups:
            outs = [_to_ticket_out(item) for item in lookups[:5]]
            matched_code = outs[0].ticket_code
            summary = self._describe_ticket(outs[0])
            if len(outs) > 1:
                summary = f"{summary} Hệ thống tìm thấy {len(outs)} phiếu sửa chữa của bạn."
            result = TrackingResult(
                tickets=outs,
                missingInformation=[],
                explanation=summary[:600],
            )
        else:
            need_clarification = True
            if "ticketCode" not in missing_info:
                missing_info.append("ticketCode")
            result = TrackingResult(
                tickets=[],
                missingInformation=["ticketCode"],
                explanation=(
                    "Vui lòng cung cấp mã phiếu sửa chữa (ví dụ: RS-20261109-001) "
                    "để hệ thống tra cứu tiến độ."
                ),
            )

        elapsed_ms = int((time.perf_counter() - started) * 1000)
        timings = dict(state.get("timings") or {})
        timings[AgentName.TRACKING.value] = elapsed_ms
        executed = list(state.get("executed_agents") or [])
        if AgentName.TRACKING.value not in executed:
            executed.append(AgentName.TRACKING.value)

        return {
            "tracking_result": result,
            "ticket_code": matched_code,
            "identified_entities": _merge_entities(state, ticket_code=matched_code),
            "need_clarification": bool(state.get("need_clarification") or need_clarification),
            "missing_information": missing_info,
            "executed_agents": executed,
            "timings": timings,
            "errors": list(state.get("errors") or []),
        }

    @staticmethod
    def _describe_ticket(ticket: TrackingTicketOut) -> str:
        text = (
            f"Phiếu {ticket.ticket_code} ({ticket.device_label}) "
            f"hiện đang ở trạng thái: {ticket.status_label}."
        )
        if ticket.estimated_completion:
            text = f"{text} Dự kiến hoàn tất: {ticket.estimated_completion}."
        return text[:600]


_tracking_agent = TrackingAgent()


async def tracking_node(state: AgentState) -> Dict[str, object]:
    """Node không ném lỗi ra ngoài graph — lỗi cục bộ đi vào `errors[]`."""
    try:
        return await _tracking_agent.run(state)
    except Exception as exc:  # pragma: no cover
        logger.exception("Tracking agent lỗi: %s", exc)
        errors = list(state.get("errors") or [])
        errors.append(
            AgentError(
                agent=AgentName.TRACKING.value,
                code="TRACKING_FAILED",
                message="Không thể tra cứu tiến độ sửa chữa lúc này.",
            )
        )
        executed = list(state.get("executed_agents") or [])
        if AgentName.TRACKING.value not in executed:
            executed.append(AgentName.TRACKING.value)
        timings = dict(state.get("timings") or {})
        timings.setdefault(AgentName.TRACKING.value, 0)
        return {"errors": errors, "executed_agents": executed, "timings": timings}


def tracking_summary(result: Optional[TrackingResult]) -> str:
    """Câu tóm tắt dùng cho `reason`."""
    if result is None or not result.tickets:
        return ""
    return result.explanation
