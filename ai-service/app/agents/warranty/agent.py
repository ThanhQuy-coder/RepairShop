"""
Warranty Agent — TASK-10.

Chức năng:
1. Tra cứu thông tin phiếu bảo hành từ `context.warrantyLookups` theo `warrantyCode`
   hoặc danh sách bảo hành của khách hàng đã đăng nhập (`CUSTOMER`).
2. Giải đáp chính sách & thời hạn gói bảo hành từ `context.warrantyPolicies`.
3. Kiểm tra hết hạn (theo `endDate` và `referenceDate`) và trạng thái `Voided`.
4. Cảnh báo điều khoản loại trừ vật lý / vào nước / tự ý tháo mở (`requiresVerification=True`).
5. Yêu cầu làm rõ (`need_clarification=True`) nếu thiếu `warrantyCode` khi tra cứu cá nhân.
"""

from __future__ import annotations

import logging
import re
import time
from datetime import date
from typing import Dict, List, Optional

from app.core.text import contains_term_flexible
from app.schemas.agent_models import (
    AgentError,
    AgentName,
    IdentifiedEntities,
    MasWarrantyPolicy,
    MasWarrantyTicketLookup,
    WarrantyLookupOut,
    WarrantyPackageOut,
    WarrantyResult,
)
from app.schemas.state_models import AgentState

logger = logging.getLogger(__name__)

WARRANTY_CODE_RE = re.compile(
    r"\b(?:BH|WR|WAR)(?:[-_][A-Z0-9]{2,15}(?:[-_][A-Z0-9]{1,15})*|(?=[A-Z0-9]*\d)[A-Z0-9]{3,20})\b",
    re.IGNORECASE,
)

_EXCLUSION_KEYWORDS = (
    "rơi",
    "vỡ",
    "nứt",
    "va đập",
    "vào nước",
    "ngấm nước",
    "dính nước",
    "chập cháy",
    "cong vênh",
    "cạy mở",
    "tự tháo",
    "mất tem",
    "rách tem",
)

_POLICY_KEYWORDS = (
    "chính sách",
    "gói bảo hành",
    "bao lâu",
    "mấy tháng",
    "thời hạn",
    "điều khoản",
    "điều kiện bảo hành",
    "quy định bảo hành",
    "bảo hành những gì",
)

_PERSONAL_LOOKUP_KEYWORDS = (
    "mã bảo hành",
    "phiếu bảo hành",
    "còn bảo hành",
    "hết bảo hành",
    "hạn bảo hành",
    "tra cứu bảo hành",
    "kiểm tra bảo hành",
    "bảo hành của tôi",
    "máy tôi còn bảo hành",
    "máy của tôi",
    "của mình",
)

_EXCLUSION_CAVEAT = (
    "Lưu ý: Các hư hỏng do rơi vỡ, va đập, vào nước hoặc tự ý tháo mở "
    "không thuộc phạm vi bảo hành miễn phí tiêu chuẩn; kỹ thuật viên cần "
    "kiểm tra trực tiếp tình trạng máy để xác nhận."
)


def extract_warranty_code(state: AgentState) -> Optional[str]:
    explicit = (state.get("warranty_code") or "").strip()
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
    match = WARRANTY_CODE_RE.search(combined)
    return match.group(0).strip().upper() if match else None


def _today_iso(state: AgentState) -> str:
    ref = (state.get("reference_date") or "").strip()
    if ref:
        try:
            return date.fromisoformat(ref).isoformat()
        except ValueError:
            pass
    return date.today().isoformat()


def _has_exclusion_risk(text: str) -> bool:
    return any(contains_term_flexible(text, kw) for kw in _EXCLUSION_KEYWORDS)


def _is_policy_inquiry(text: str) -> bool:
    return any(contains_term_flexible(text, kw) for kw in _POLICY_KEYWORDS)


def _is_personal_lookup(text: str) -> bool:
    return any(contains_term_flexible(text, kw) for kw in _PERSONAL_LOOKUP_KEYWORDS)


def _to_policy_out(policy: MasWarrantyPolicy) -> WarrantyPackageOut:
    return WarrantyPackageOut(
        packageId=policy.package_id,
        name=policy.name,
        durationMonths=policy.duration_months,
        terms=policy.terms,
        description=policy.description,
    )


def _to_lookup_out(lookup: MasWarrantyTicketLookup, today_iso: str) -> WarrantyLookupOut:
    expired = bool(lookup.is_expired or lookup.end_date < today_iso)
    if lookup.status == "Voided":
        status = "Voided"
    elif expired or lookup.status == "Expired":
        status = "Expired"
        expired = True
    else:
        status = "Active"

    return WarrantyLookupOut(
        warrantyCode=lookup.warranty_code,
        ticketCode=lookup.ticket_code,
        deviceLabel=lookup.device_label,
        status=status,
        startDate=lookup.start_date,
        endDate=lookup.end_date,
        isExpired=expired,
        packageName=lookup.package_name,
        terms=lookup.terms,
    )


def _merge_entities(
    state: AgentState, *, warranty_code: Optional[str] = None, ticket_code: Optional[str] = None
) -> IdentifiedEntities:
    existing = state.get("identified_entities")
    data = existing.model_dump() if isinstance(existing, IdentifiedEntities) else {}
    device = state.get("device") or {}
    data.setdefault("brand", device.get("brand"))
    data.setdefault("model", device.get("model"))
    data.setdefault("device_type", device.get("deviceType"))
    if warranty_code:
        data["warranty_code"] = warranty_code
    if ticket_code:
        data["ticket_code"] = ticket_code
    return IdentifiedEntities.model_validate(data)


class WarrantyAgent:
    """Agent tra cứu và tư vấn bảo hành từ dữ liệu thật của Backend."""

    async def run(self, state: AgentState) -> Dict[str, object]:
        started = time.perf_counter()
        text = " ".join(
            part
            for part in (
                state.get("user_message") or "",
                state.get("issue_description") or "",
            )
            if part
        ).strip()

        today_iso = _today_iso(state)
        code = extract_warranty_code(state)

        lookups = [
            MasWarrantyTicketLookup.model_validate(item)
            for item in (state.get("warranty_lookups") or [])
        ]
        policies = [
            MasWarrantyPolicy.model_validate(item)
            for item in (state.get("warranty_policies") or [])
            if item.get("isActive", True)
        ]

        exclusion_risk = _has_exclusion_risk(text)
        policy_query = _is_policy_inquiry(text)
        personal_query = _is_personal_lookup(text)

        need_clarification = False
        missing_info: List[str] = list(state.get("missing_information") or [])
        matched_warranty_code: Optional[str] = code
        matched_ticket_code: Optional[str] = state.get("ticket_code")

        if code:
            matched = next(
                (item for item in lookups if item.warranty_code.upper() == code.upper()),
                None,
            )
            if matched is None:
                explanation = (
                    f"Không tìm thấy thông tin bảo hành cho mã {code}. "
                    "Vui lòng kiểm tra lại mã bảo hành hoặc phiếu sửa chữa."
                )
                result = WarrantyResult(
                    policies=[],
                    warranties=[],
                    missingInformation=[],
                    requiresVerification=True,
                    explanation=explanation[:600],
                )
            else:
                out = _to_lookup_out(matched, today_iso)
                matched_warranty_code = out.warranty_code
                matched_ticket_code = out.ticket_code
                policy_outs = (
                    [_to_policy_out(p) for p in policies if p.name == out.package_name]
                    if out.package_name
                    else []
                )
                if not policy_outs and policies and exclusion_risk:
                    policy_outs = [_to_policy_out(p) for p in policies[:2]]

                parts = [self._describe_record(out)]
                if exclusion_risk:
                    parts.append(_EXCLUSION_CAVEAT)
                else:
                    parts.append("Kỹ thuật viên sẽ kiểm tra thực tế máy để xác nhận điều kiện bảo hành.")

                result = WarrantyResult(
                    policies=policy_outs,
                    warranties=[out],
                    missingInformation=[],
                    requiresVerification=True,
                    explanation=" ".join(parts).strip()[:600],
                )

        elif lookups:
            outs = [_to_lookup_out(item, today_iso) for item in lookups[:5]]
            matched_warranty_code = outs[0].warranty_code
            matched_ticket_code = outs[0].ticket_code
            parts = [self._describe_record(outs[0])]
            if len(outs) > 1:
                parts.append(f"Tìm thấy tổng cộng {len(outs)} phiếu bảo hành của bạn.")
            if exclusion_risk:
                parts.append(_EXCLUSION_CAVEAT)
            else:
                parts.append("Kỹ thuật viên sẽ kiểm tra thực tế máy để xác nhận điều kiện bảo hành.")

            policy_outs = [_to_policy_out(p) for p in policies[:3]] if policy_query else []
            result = WarrantyResult(
                policies=policy_outs,
                warranties=outs,
                missingInformation=[],
                requiresVerification=True,
                explanation=" ".join(parts).strip()[:600],
            )

        elif policies and (policy_query or not personal_query):
            policy_outs = [_to_policy_out(p) for p in policies[:5]]
            first = policy_outs[0]
            parts = [
                f"Cửa hàng hiện áp dụng gói {first.name} ({first.duration_months} tháng): {first.terms}."
            ]
            if len(policy_outs) > 1:
                parts.append(f"Có {len(policy_outs)} gói bảo hành đang áp dụng.")
            if exclusion_risk:
                parts.append(_EXCLUSION_CAVEAT)
            elif personal_query:
                parts.append("Để kiểm tra hạn bảo hành riêng của máy, vui lòng cung cấp mã bảo hành (ví dụ: BH-...).")

            result = WarrantyResult(
                policies=policy_outs,
                warranties=[],
                missingInformation=[],
                requiresVerification=True,
                explanation=" ".join(parts).strip()[:600],
            )

        else:
            need_clarification = True
            if "warrantyCode" not in missing_info:
                missing_info.append("warrantyCode")
            parts = [
                "Vui lòng cung cấp mã bảo hành (ví dụ: BH-20261109-001) để hệ thống kiểm tra thời hạn và điều kiện áp dụng."
            ]
            if exclusion_risk:
                parts.append(_EXCLUSION_CAVEAT)
            result = WarrantyResult(
                policies=[],
                warranties=[],
                missingInformation=["warrantyCode"],
                requiresVerification=True,
                explanation=" ".join(parts).strip()[:600],
            )

        elapsed_ms = int((time.perf_counter() - started) * 1000)
        timings = dict(state.get("timings") or {})
        timings[AgentName.WARRANTY.value] = elapsed_ms
        executed = list(state.get("executed_agents") or [])
        if AgentName.WARRANTY.value not in executed:
            executed.append(AgentName.WARRANTY.value)

        return {
            "warranty_result": result,
            "warranty_code": matched_warranty_code,
            "ticket_code": matched_ticket_code,
            "identified_entities": _merge_entities(
                state,
                warranty_code=matched_warranty_code,
                ticket_code=matched_ticket_code,
            ),
            "need_clarification": bool(state.get("need_clarification") or need_clarification),
            "missing_information": missing_info,
            "executed_agents": executed,
            "timings": timings,
            "errors": list(state.get("errors") or []),
        }

    @staticmethod
    def _describe_record(item: WarrantyLookupOut) -> str:
        if item.status == "Voided":
            return (
                f"Phiếu bảo hành {item.warranty_code} ({item.device_label}) "
                "hiện ở trạng thái đã bị hủy/vô hiệu lực."
            )
        if item.is_expired or item.status == "Expired":
            return (
                f"Phiếu bảo hành {item.warranty_code} ({item.device_label}) "
                f"đã hết hạn vào ngày {item.end_date}."
            )
        pkg = f" (gói {item.package_name})" if item.package_name else ""
        return (
            f"Phiếu bảo hành {item.warranty_code} ({item.device_label}){pkg} "
            f"đang còn hiệu lực đến hết ngày {item.end_date}."
        )


_warranty_agent = WarrantyAgent()


async def warranty_node(state: AgentState) -> Dict[str, object]:
    """Node không ném lỗi ra ngoài graph — lỗi cục bộ đi vào `errors[]`."""
    try:
        return await _warranty_agent.run(state)
    except Exception as exc:  # pragma: no cover
        logger.exception("Warranty agent lỗi: %s", exc)
        errors = list(state.get("errors") or [])
        errors.append(
            AgentError(
                agent=AgentName.WARRANTY.value,
                code="WARRANTY_FAILED",
                message="Không thể kiểm tra thông tin bảo hành lúc này.",
            )
        )
        executed = list(state.get("executed_agents") or [])
        if AgentName.WARRANTY.value not in executed:
            executed.append(AgentName.WARRANTY.value)
        timings = dict(state.get("timings") or {})
        timings.setdefault(AgentName.WARRANTY.value, 0)
        return {"errors": errors, "executed_agents": executed, "timings": timings}


def warranty_summary(result: Optional[WarrantyResult]) -> str:
    """Câu tóm tắt ngắn dùng cho `reason`."""
    if result is None:
        return ""
    if result.warranties or result.policies:
        return result.explanation
    return ""
