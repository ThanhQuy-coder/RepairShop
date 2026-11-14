"""Routing thuần hàm cho MAS (Task 2.1, mở rộng TASK-10/11/13) — dễ test, không side effect."""

from __future__ import annotations

from typing import Dict, Iterable, List, Optional

from app.schemas.agent_models import AgentName, Intent

_ORDER: tuple[AgentName, ...] = (
    AgentName.WARRANTY,
    AgentName.TRACKING,
    AgentName.DIAGNOSIS,
    AgentName.QUOTATION,
    AgentName.APPOINTMENT,
)

_NODE_BY_AGENT: Dict[str, str] = {
    AgentName.DIAGNOSIS.value: "diagnosis",
    AgentName.QUOTATION.value: "quotation",
    AgentName.APPOINTMENT.value: "appointment",
    AgentName.WARRANTY.value: "warranty",
    AgentName.TRACKING.value: "tracking",
}

AGGREGATE_NODE = "aggregate"


def build_plan(
    intent: Intent,
    allow_diagnosis: bool,
    allow_quotation: bool,
    allow_appointment: bool,
    allow_warranty: bool = False,
    allow_tracking: bool = False,
) -> List[str]:
    """
    Lập kế hoạch chạy agent từ intent + quyền Backend cấp.

    Bảng quyết định:
    - DIAGNOSIS: [D] (+Q nếu allowQuotation) (+A nếu allowAppointment)
    - QUOTATION: [Q] nếu được phép, ngược lại fallback agent được phép đầu tiên
    - APPOINTMENT: [A] nếu được phép, ngược lại fallback
    - WARRANTY: [WARRANTY] nếu được phép, ngược lại []
    - TRACKING: [TRACKING] nếu được phép, ngược lại []
    - UNKNOWN: [] => aggregate trả lời làm rõ
    """
    if intent is Intent.DIAGNOSIS:
        plan: List[str] = []
        if allow_diagnosis:
            plan.append(AgentName.DIAGNOSIS.value)
        if allow_quotation:
            plan.append(AgentName.QUOTATION.value)
        if allow_appointment:
            plan.append(AgentName.APPOINTMENT.value)
        # Intent chẩn đoán luôn phải được phục vụ, kể cả khi Backend không bật cờ.
        return plan or [AgentName.DIAGNOSIS.value]

    if intent is Intent.QUOTATION:
        if allow_quotation:
            return [AgentName.QUOTATION.value]
        return _first_allowed(allow_diagnosis, allow_quotation, allow_appointment)

    if intent is Intent.APPOINTMENT:
        if allow_appointment:
            return [AgentName.APPOINTMENT.value]
        return _first_allowed(allow_diagnosis, allow_quotation, allow_appointment)

    if intent is Intent.WARRANTY:
        return [AgentName.WARRANTY.value] if allow_warranty else []

    if intent is Intent.TRACKING:
        return [AgentName.TRACKING.value] if allow_tracking else []

    return []


def _first_allowed(
    allow_diagnosis: bool, allow_quotation: bool, allow_appointment: bool
) -> List[str]:
    if allow_diagnosis:
        return [AgentName.DIAGNOSIS.value]
    if allow_quotation:
        return [AgentName.QUOTATION.value]
    if allow_appointment:
        return [AgentName.APPOINTMENT.value]
    return []


def next_agent(plan: Iterable[str], executed: Iterable[str]) -> Optional[str]:
    """Agent kế tiếp trong plan chưa chạy; None nếu đã xong."""
    done = set(executed)
    for agent in plan:
        if agent not in done:
            return agent
    return None


def node_for(agent: Optional[str]) -> str:
    if agent is None:
        return AGGREGATE_NODE
    return _NODE_BY_AGENT.get(agent, AGGREGATE_NODE)


def _route_next(state: dict) -> str:
    return node_for(next_agent(state.get("plan", []), state.get("executed_agents", [])))


def route_after_coordinator(state: dict) -> str:
    return _route_next(state)


def route_after_diagnosis(state: dict) -> str:
    return _route_next(state)


def route_after_quotation(state: dict) -> str:
    return _route_next(state)


def route_after_appointment(state: dict) -> str:
    return _route_next(state)


def route_after_warranty(state: dict) -> str:
    return _route_next(state)


def route_after_tracking(state: dict) -> str:
    return _route_next(state)
