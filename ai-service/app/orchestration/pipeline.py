"""
Functional Pipeline 6 bước tường minh — TASK-13.

Thay thế vòng lặp state-merge phức tạp khi chạy request bằng chuỗi hàm tuần tự rõ ràng:
1. `step_normalize`: chuẩn bị state đầu vào và chuẩn hoá entity từ request.
2. `step_classify_and_route`: gọi Coordinator phân loại Intent (Rule-first + ML) & lập `plan`.
3. `step_execute_plan`: thực thi tuần tự các Agent theo `plan` (DIAGNOSIS / QUOTATION / APPOINTMENT / WARRANTY / TRACKING).
4. `step_aggregate`: tổng hợp trạng thái (`SUCCESS`, `PARTIAL`, `NEED_CLARIFICATION`, `NO_MATCH`, `OUT_OF_SCOPE`, `ERROR`) và lý do.
5. `step_validate_and_respond`: hậu kiểm zero-trust đối chiếu context Backend & dựng `AgentResponse`.
6. `run_pipeline`: điểm vào hợp nhất với lưới an toàn cuối.
"""

from __future__ import annotations

import logging
from typing import Any, Awaitable, Callable, Dict

from app.agents.appointment import agent as appointment_module
from app.agents.coordinator.agent import coordinator_node
from app.agents.diagnosis import agent as diagnosis_module
from app.agents.quotation import agent as quotation_module
from app.agents.tracking import agent as tracking_module
from app.agents.warranty import agent as warranty_module
from app.orchestration.postprocess import aggregate_node, validate_node
from app.schemas.agent_models import AgentError, AgentName, AgentRequest, AgentResponse
from app.schemas.state_models import AgentState

logger = logging.getLogger(__name__)

AgentNodeFn = Callable[[AgentState], Awaitable[Dict[str, object]]]


def step_normalize(request: AgentRequest) -> AgentState:
    """Bước 1: Chuẩn hoá đầu vào từ `AgentRequest` sang `AgentState`."""
    return {
        "request_id": str(request.request_id),
        "user_message": request.message,
        "actor_type": request.actor_type.value,
        "device": {
            "deviceType": request.device.device_type.value,
            "brand": request.device.brand,
            "model": request.device.model,
        },
        "issue_description": request.issue_description,
        "available_services": [
            service.model_dump(by_alias=True)
            for service in request.context.available_services
        ],
        "available_parts": [
            part.model_dump(by_alias=True)
            for part in request.context.available_parts
        ],
        "available_slots": [
            slot.model_dump(by_alias=True)
            for slot in request.context.available_slots
        ],
        "warranty_policies": [
            policy.model_dump(by_alias=True)
            for policy in request.context.warranty_policies
        ],
        "warranty_lookups": [
            lookup.model_dump(by_alias=True)
            for lookup in request.context.warranty_lookups
        ],
        "tracking_lookups": [
            lookup.model_dump(by_alias=True)
            for lookup in request.context.tracking_lookups
        ],
        "diagnosis_symptoms": [
            symptom.model_dump(by_alias=True)
            for symptom in request.context.diagnosis_symptoms
        ],
        "options": request.options.model_dump(by_alias=True),
        "customer_info": request.customer_info.model_dump(by_alias=True),
        "reference_date": request.reference_date,
        "ticket_code": request.ticket_code,
        "warranty_code": request.warranty_code,
        "executed_agents": [],
        "secondary_intents": [],
        "errors": [],
        "timings": {},
        "llm_calls": 0,
        "fallback_used": False,
        "out_of_scope": False,
        "need_clarification": False,
        "missing_information": [],
    }


async def step_classify_and_route(state: AgentState) -> AgentState:
    """Bước 2: Phân loại Intent (Hybrid Rule + ML) và lập kế hoạch agent."""
    updated = dict(state)
    updated.update(await coordinator_node(updated))
    return updated


def _dispatch_table() -> Dict[str, AgentNodeFn]:
    # Gọi qua module để tôn trọng monkeypatch trong unit test (`monkeypatch.setattr(mod, "_x_agent", ...)`).
    return {
        AgentName.DIAGNOSIS.value: diagnosis_module.diagnosis_node,
        AgentName.QUOTATION.value: quotation_module.quotation_node,
        AgentName.APPOINTMENT.value: appointment_module.appointment_node,
        AgentName.WARRANTY.value: warranty_module.warranty_node,
        AgentName.TRACKING.value: tracking_module.tracking_node,
    }


async def step_execute_plan(state: AgentState) -> AgentState:
    """Bước 3: Thực thi lần lượt các agent trong `plan`."""
    updated = dict(state)
    dispatch = _dispatch_table()
    for agent_name in list(updated.get("plan") or []):
        node_fn = dispatch.get(agent_name)
        if node_fn is None:
            logger.warning("Pipeline bỏ qua agent không hợp lệ: %s", agent_name)
            continue
        try:
            delta = await node_fn(updated)
            updated.update(delta)
        except Exception as exc:  # pragma: no cover
            logger.exception("Pipeline lỗi khi chạy %s: %s", agent_name, exc)
            errors = list(updated.get("errors") or [])
            errors.append(
                AgentError(
                    agent=agent_name,
                    code=f"{agent_name}_FAILED",
                    message=f"Không thể hoàn tất bước {agent_name}.",
                )
            )
            executed = list(updated.get("executed_agents") or [])
            if agent_name not in executed:
                executed.append(agent_name)
            updated["errors"] = errors
            updated["executed_agents"] = executed
    return updated


async def step_aggregate(state: AgentState) -> AgentState:
    """Bước 4: Tổng hợp status và reason từ kết quả các agent."""
    updated = dict(state)
    updated.update(await aggregate_node(updated))
    return updated


async def step_validate_and_respond(state: AgentState) -> AgentResponse:
    """Bước 5 & 6: Hậu kiểm zero-trust và tạo `AgentResponse` cuối."""
    updated = dict(state)
    updated.update(await validate_node(updated))
    response = updated.get("final_response")
    if response is None:
        logger.error("Pipeline không tạo final_response cho request %s", state.get("request_id"))
        return AgentResponse.error(
            request_id=state["request_id"],
            reason="Không thể tổng hợp kết quả tư vấn. Vui lòng thử lại sau.",
        )
    return response


async def run_pipeline(request: AgentRequest) -> AgentResponse:
    """Chạy đầy đủ 6 bước Functional Pipeline cho một yêu cầu MAS."""
    try:
        state = step_normalize(request)
        state = await step_classify_and_route(state)
        state = await step_execute_plan(state)
        state = await step_aggregate(state)
        return await step_validate_and_respond(state)
    except Exception as exc:  # pragma: no cover
        logger.exception("Pipeline thất bại ở lớp ngoài: %s", exc)
        return AgentResponse.error(
            request.request_id,
            "Không thể xử lý yêu cầu tư vấn lúc này. Vui lòng thử lại sau.",
        )
