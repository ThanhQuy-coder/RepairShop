"""Task 2.1 — Routing thuần hàm: build_plan, next_agent, route_after_*."""

from __future__ import annotations

import pytest

from app.orchestration.routing import (
    build_plan,
    next_agent,
    node_for,
    route_after_appointment,
    route_after_coordinator,
    route_after_diagnosis,
    route_after_quotation,
)
from app.schemas.agent_models import Intent


@pytest.mark.parametrize(
    "intent,allow_d,allow_q,allow_a,expected",
    [
        (Intent.DIAGNOSIS, True, False, False, ["DIAGNOSIS"]),
        (Intent.DIAGNOSIS, True, True, False, ["DIAGNOSIS", "QUOTATION"]),
        (
            Intent.DIAGNOSIS,
            True,
            True,
            True,
            ["DIAGNOSIS", "QUOTATION", "APPOINTMENT"],
        ),
        (Intent.QUOTATION, True, True, False, ["QUOTATION"]),
        (Intent.APPOINTMENT, True, True, True, ["APPOINTMENT"]),
        (Intent.TRACKING, True, True, True, []),
        (Intent.UNKNOWN, True, True, True, []),
    ],
)
def test_build_plan_matches_routing_table(intent, allow_d, allow_q, allow_a, expected):
    assert build_plan(intent, allow_d, allow_q, allow_a) == expected


def test_build_plan_falls_back_when_requested_agent_is_disabled():
    # QUOTATION bị tắt nhưng DIAGNOSIS được bật => chạy DIAGNOSIS thay vì không làm gì.
    assert build_plan(Intent.QUOTATION, True, False, False) == ["DIAGNOSIS"]
    # Không agent nào được bật => plan rỗng.
    assert build_plan(Intent.QUOTATION, False, False, False) == []


def test_diagnosis_intent_always_gets_served_even_if_flag_is_false():
    assert build_plan(Intent.DIAGNOSIS, False, False, False) == ["DIAGNOSIS"]


def test_next_agent_returns_first_unexecuted():
    plan = ["DIAGNOSIS", "QUOTATION", "APPOINTMENT"]
    assert next_agent(plan, []) == "DIAGNOSIS"
    assert next_agent(plan, ["DIAGNOSIS"]) == "QUOTATION"
    assert next_agent(plan, ["DIAGNOSIS", "QUOTATION"]) == "APPOINTMENT"
    assert next_agent(plan, plan) is None


def test_node_for_maps_agent_to_node_name():
    assert node_for("DIAGNOSIS") == "diagnosis"
    assert node_for("QUOTATION") == "quotation"
    assert node_for("APPOINTMENT") == "appointment"
    assert node_for(None) == "aggregate"


def test_route_after_coordinator_follows_plan():
    state = {"plan": ["DIAGNOSIS", "QUOTATION"], "executed_agents": []}
    assert route_after_coordinator(state) == "diagnosis"

    state = {"plan": ["DIAGNOSIS", "QUOTATION"], "executed_agents": ["DIAGNOSIS"]}
    assert route_after_diagnosis(state) == "quotation"

    state = {"plan": [], "executed_agents": []}
    assert route_after_coordinator(state) == "aggregate"


def test_route_after_appointment_always_aggregates():
    state = {"plan": ["APPOINTMENT"], "executed_agents": ["APPOINTMENT"]}
    assert route_after_appointment(state) == "aggregate"
