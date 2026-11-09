"""
LangGraph workflow của MAS (Task 2.1).

Sơ đồ (rút gọn theo HUONG_DAN_TRIEN_KHAI_MAS_4_NGAY.md mục 4.5):

    START -> coordinator -> (diagnosis | quotation | appointment | aggregate)
          -> ... -> aggregate -> validate -> END

Node Quotation/Appointment là agent thật (Task 3.1/3.2); graph được compile MỘT LẦN
khi module được import; `app/api/agents.py` import module này theo kiểu lazy để `/health`
và `/v1/advisory` legacy không phải chịu cold start của LangGraph.
"""

from __future__ import annotations

import logging
from typing import Any, Dict

from langgraph.graph import END, START, StateGraph

from app.agents.appointment.agent import appointment_node
from app.agents.coordinator.agent import coordinator_node
from app.agents.diagnosis.agent import diagnosis_node
from app.agents.quotation.agent import quotation_node
from app.orchestration.postprocess import aggregate_node, validate_node
from app.orchestration.routing import (
    AGGREGATE_NODE,
    route_after_appointment,
    route_after_coordinator,
    route_after_diagnosis,
    route_after_quotation,
)
from app.schemas.agent_models import AgentRequest, AgentResponse
from app.schemas.state_models import AgentState

logger = logging.getLogger(__name__)

_ROUTE_MAP = {
    "diagnosis": "diagnosis",
    "quotation": "quotation",
    "appointment": "appointment",
    AGGREGATE_NODE: AGGREGATE_NODE,
}


def build_graph():
    graph = StateGraph(AgentState)

    graph.add_node("coordinator", coordinator_node)
    graph.add_node("diagnosis", diagnosis_node)
    graph.add_node("quotation", quotation_node)
    graph.add_node("appointment", appointment_node)
    graph.add_node("aggregate", aggregate_node)
    graph.add_node("validate", validate_node)

    graph.add_edge(START, "coordinator")

    graph.add_conditional_edges(
        "coordinator", route_after_coordinator, _ROUTE_MAP
    )
    graph.add_conditional_edges("diagnosis", route_after_diagnosis, _ROUTE_MAP)
    graph.add_conditional_edges("quotation", route_after_quotation, _ROUTE_MAP)
    graph.add_conditional_edges("appointment", route_after_appointment, _ROUTE_MAP)

    graph.add_edge("aggregate", "validate")
    graph.add_edge("validate", END)

    return graph.compile()


compiled_graph = build_graph()


def _initial_state(request: AgentRequest) -> Dict[str, Any]:
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
            service.model_dump(by_alias=True) for service in request.context.available_services
        ],
        "available_parts": [
            part.model_dump(by_alias=True) for part in request.context.available_parts
        ],
        "available_slots": [
            slot.model_dump(by_alias=True) for slot in request.context.available_slots
        ],
        "options": request.options.model_dump(by_alias=True),
        "customer_info": request.customer_info.model_dump(by_alias=True),
        "reference_date": request.reference_date,
        "executed_agents": [],
        "errors": [],
        "timings": {},
        "llm_calls": 0,
        "fallback_used": False,
        "out_of_scope": False,
    }


async def run_agents(request: AgentRequest) -> AgentResponse:
    """Chạy graph và trả về response cuối. Node tự bắt lỗi; đây là lưới an toàn cuối."""
    final_state = await compiled_graph.ainvoke(_initial_state(request))
    response = final_state.get("final_response")
    if response is None:
        logger.error("Graph không tạo final_response cho request %s", request.request_id)
        return AgentResponse.error(
            request.request_id,
            "Không thể tổng hợp kết quả tư vấn. Vui lòng thử lại sau.",
        )
    return response
