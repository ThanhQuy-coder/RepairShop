"""Quotation Agent (TASK-09).

Nguyên tắc cốt lõi: **mọi con số đến từ context Backend, không từ LLM**.
Agent không gọi LLM (giữ ngân sách 1 call/request cho Diagnosis) và không bao giờ
tự sinh giá, tự cộng thêm phí, hay dùng `CostPrice`.
"""

from __future__ import annotations

import logging
import time
from typing import Dict, List, Optional

from app.retrieval.retrieval_service import retrieve_candidates
from app.schemas.agent_models import (
    AgentError,
    AgentName,
    MasPart,
    MasService,
    QuotationItem,
    QuotationResult,
)
from app.schemas.request_models import (
    AdvisoryContext,
    AvailablePart,
    AvailableService,
    DeviceInfo,
)
from app.schemas.state_models import AgentState

logger = logging.getLogger(__name__)

MAX_SERVICES = 3
MAX_PARTS = 2


def _services(state: AgentState) -> Dict[str, MasService]:
    return {
        item.service_id: item
        for item in (MasService.model_validate(raw) for raw in state.get("available_services") or [])
    }


def _parts(state: AgentState) -> Dict[str, MasPart]:
    return {
        item.part_id: item
        for item in (MasPart.model_validate(raw) for raw in state.get("available_parts") or [])
    }


def _select_from_retrieval(
    state: AgentState,
) -> tuple[List[str], List[str], List[str], bool]:
    """Khi không có Diagnosis (intent QUOTATION thuần): chọn dịch vụ sát nhất từ retrieval."""
    device = DeviceInfo.model_validate(state["device"])
    context = AdvisoryContext(
        availableServices=[
            AvailableService.model_validate(raw)
            for raw in state.get("available_services") or []
        ],
        availableParts=[
            AvailablePart.model_validate(raw) for raw in state.get("available_parts") or []
        ],
    )
    issue = state.get("issue_description") or state.get("user_message") or ""
    retrieval = retrieve_candidates(device, issue, context)

    oos_notes = [
        f"{part.name} hiện hết hàng nên chưa được đưa vào báo giá."
        for part in retrieval.out_of_stock_parts[:MAX_PARTS]
    ]

    best_score = max(retrieval.service_scores.values(), default=0)
    if best_score <= 0:
        return [], [], oos_notes, retrieval.is_vague_issue

    threshold = max(1, int(best_score * 0.75))
    service_ids = [
        service.service_id
        for service in retrieval.candidate_services
        if retrieval.service_scores.get(service.service_id, 0) >= threshold
    ][:MAX_SERVICES]
    return service_ids, [], oos_notes, retrieval.is_vague_issue


class QuotationAgent:
    async def run(self, state: AgentState) -> Dict[str, object]:
        started = time.perf_counter()

        services = _services(state)
        parts = _parts(state)
        diagnosis = state.get("diagnosis_result")

        assumptions: List[str] = []
        is_vague = False

        if diagnosis and (diagnosis.suggested_services or diagnosis.suggested_parts):
            service_ids = [item.service_id for item in diagnosis.suggested_services][:MAX_SERVICES]
            part_ids = [item.part_id for item in diagnosis.suggested_parts][:MAX_PARTS]
        else:
            service_ids, part_ids, oos_notes, is_vague = _select_from_retrieval(state)
            assumptions.extend(oos_notes)

        service_ids = list(dict.fromkeys(service_ids))
        part_ids = list(dict.fromkeys(part_ids))

        items: List[QuotationItem] = []

        for service_id in service_ids:
            service = services.get(service_id)
            if service is None:
                logger.warning("Quotation bỏ serviceId lạ '%s'.", service_id)
                continue
            price = float(service.base_price)
            items.append(
                QuotationItem(
                    itemId=service.service_id,
                    itemType="SERVICE",
                    name=service.name,
                    quantity=1,
                    unitPrice=price,
                    lineTotal=price,
                )
            )

        for part_id in part_ids:
            part = parts.get(part_id)
            if part is None:
                logger.warning("Quotation bỏ partId lạ '%s'.", part_id)
                continue
            if not part.in_stock or (part.quantity_on_hand is not None and part.quantity_on_hand <= 0):
                assumptions.append(
                    f"{part.name} hiện hết hàng nên chưa được đưa vào báo giá."
                )
                continue
            price = float(part.unit_price)
            items.append(
                QuotationItem(
                    itemId=part.part_id,
                    itemType="PART",
                    name=part.name,
                    quantity=1,
                    unitPrice=price,
                    lineTotal=price,
                )
            )

        assumptions = list(dict.fromkeys(assumptions))
        estimated_total = round(sum(item.line_total for item in items), 2)

        if any(item.item_type == "SERVICE" for item in items) and any(
            item.item_type == "PART" for item in items
        ):
            assumptions.append(
                "Giá dịch vụ và giá linh kiện có thể trùng một phần công thợ; "
                "nhân viên sẽ chốt khi kiểm tra máy."
            )

        need_clarification = bool(state.get("need_clarification"))
        missing_info = list(state.get("missing_information") or [])

        if items:
            explanation = (
                f"Báo giá tham khảo gồm {len(items)} hạng mục theo bảng giá hiện hành. "
                "Đây không phải báo giá chính thức."
            )
        elif is_vague and diagnosis is None:
            need_clarification = True
            missing_item = "dịch vụ hoặc triệu chứng cần báo giá (ví dụ: thay pin, ép kính)"
            if missing_item not in missing_info:
                missing_info.append(missing_item)
            explanation = (
                "Chưa rõ dịch vụ bạn cần báo giá. "
                "Vui lòng nêu rõ hạng mục hoặc tình trạng thiết bị để nhận báo giá tham khảo."
            )
        else:
            explanation = (
                "Chưa có dịch vụ hoặc linh kiện phù hợp trong dữ liệu cửa hàng để báo giá."
            )

        result = QuotationResult(
            items=items,
            estimatedTotal=estimated_total,
            currency="VND",
            assumptions=assumptions,
            explanation=explanation[:600],
        )

        elapsed_ms = int((time.perf_counter() - started) * 1000)
        timings = dict(state.get("timings") or {})
        timings[AgentName.QUOTATION.value] = elapsed_ms
        executed = list(state.get("executed_agents") or [])
        if AgentName.QUOTATION.value not in executed:
            executed.append(AgentName.QUOTATION.value)

        logger.info(
            "quotation request_id=%s items=%d total=%.0f elapsed_ms=%d",
            state.get("request_id"),
            len(items),
            estimated_total,
            elapsed_ms,
        )

        return {
            "quotation_result": result,
            "need_clarification": need_clarification,
            "missing_information": missing_info,
            "executed_agents": executed,
            "timings": timings,
        }


_quotation_agent = QuotationAgent()


async def quotation_node(state: AgentState) -> Dict[str, object]:
    """Node không được ném lỗi ra ngoài graph — lỗi cục bộ đi vào `errors[]`."""
    try:
        return await _quotation_agent.run(state)
    except Exception as exc:  # pragma: no cover - phòng lỗi bất ngờ
        logger.exception("Quotation agent lỗi: %s", exc)
        errors = list(state.get("errors") or [])
        errors.append(
            AgentError(
                agent=AgentName.QUOTATION.value,
                code="QUOTATION_FAILED",
                message="Không thể tạo báo giá tham khảo.",
            )
        )
        executed = list(state.get("executed_agents") or [])
        if AgentName.QUOTATION.value not in executed:
            executed.append(AgentName.QUOTATION.value)
        timings = dict(state.get("timings") or {})
        timings.setdefault(AgentName.QUOTATION.value, 0)
        return {"errors": errors, "executed_agents": executed, "timings": timings}


def quote_summary(result: Optional[QuotationResult]) -> str:
    """Câu tóm tắt dùng cho `reason` — không chứa dữ liệu cấu trúc."""
    if result is None or not result.items:
        return ""
    return f"Giá tham khảo: {result.estimated_total:,.0f} VND cho {len(result.items)} hạng mục."
