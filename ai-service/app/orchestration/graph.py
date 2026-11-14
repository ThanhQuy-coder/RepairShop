"""
Orchestration entrypoint & LangGraph workflow của MAS (Task 2.1, cập nhật TASK-13).

- `build_graph()` / `compiled_graph`: giữ tương thích ngược và bổ sung node `warranty` & `tracking`.
- `run_agents(request)`: chạy Functional Pipeline 6 bước (`pipeline.run_pipeline`) với hậu kiểm zero-trust.
"""

from __future__ import annotations

import logging
from typing import Dict

from langgraph.graph import END, START, StateGraph

from app.agents.appointment.agent import appointment_node
from app.agents.coordinator.agent import coordinator_node
from app.agents.diagnosis.agent import diagnosis_node
from app.agents.quotation.agent import quotation_node
from app.agents.tracking.agent import tracking_node
from app.agents.warranty.agent import warranty_node
from app.orchestration.pipeline import run_pipeline, step_normalize
from app.orchestration.postprocess import aggregate_node, validate_node
from app.orchestration.routing import (
    AGGREGATE_NODE,
    route_after_appointment,
    route_after_coordinator,
    route_after_diagnosis,
    route_after_quotation,
    route_after_tracking,
    route_after_warranty,
)
from app.schemas.agent_models import AgentRequest, AgentResponse
from app.schemas.state_models import AgentState

logger = logging.getLogger(__name__)

_ROUTE_MAP: Dict[str, str] = {
    "diagnosis": "diagnosis",
    "quotation": "quotation",
    "appointment": "appointment",
    "warranty": "warranty",
    "tracking": "tracking",
    AGGREGATE_NODE: AGGREGATE_NODE,
}


def build_graph():
    graph = StateGraph(AgentState)

    graph.add_node("coordinator", coordinator_node)
    graph.add_node("diagnosis", diagnosis_node)
    graph.add_node("quotation", quotation_node)
    graph.add_node("appointment", appointment_node)
    graph.add_node("warranty", warranty_node)
    graph.add_node("tracking", tracking_node)
    graph.add_node("aggregate", aggregate_node)
    graph.add_node("validate", validate_node)

    graph.add_edge(START, "coordinator")

    graph.add_conditional_edges("coordinator", route_after_coordinator, _ROUTE_MAP)
    graph.add_conditional_edges("diagnosis", route_after_diagnosis, _ROUTE_MAP)
    graph.add_conditional_edges("quotation", route_after_quotation, _ROUTE_MAP)
    graph.add_conditional_edges("appointment", route_after_appointment, _ROUTE_MAP)
    graph.add_conditional_edges("warranty", route_after_warranty, _ROUTE_MAP)
    graph.add_conditional_edges("tracking", route_after_tracking, _ROUTE_MAP)

    graph.add_edge("aggregate", "validate")
    graph.add_edge("validate", END)

    return graph.compile()


compiled_graph = build_graph()


def _initial_state(request: AgentRequest) -> AgentState:
    return step_normalize(request)


async def run_agents(request: AgentRequest) -> AgentResponse:
    """Chạy Functional Pipeline 6 bước và trả về `AgentResponse` đã hậu kiểm."""
    return await run_pipeline(request)
