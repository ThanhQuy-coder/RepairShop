"""Coordinator Agent (Task 2.1, mở rộng TASK-04/10/11/13) — phân loại intent và lập kế hoạch."""

from __future__ import annotations

import logging
import time
from typing import Optional

from app.agents.tracking.agent import extract_ticket_code
from app.agents.warranty.agent import extract_warranty_code
from app.ml.intent_classifier import classify_intent
from app.orchestration.routing import build_plan
from app.schemas.agent_models import IdentifiedEntities, Intent
from app.schemas.state_models import AgentState

logger = logging.getLogger(__name__)


def _initial_entities(
    state: AgentState,
    ticket_code: Optional[str],
    warranty_code: Optional[str],
) -> IdentifiedEntities:
    existing = state.get("identified_entities")
    data = existing.model_dump() if isinstance(existing, IdentifiedEntities) else {}
    device = state.get("device") or {}
    data.setdefault("brand", device.get("brand"))
    data.setdefault("model", device.get("model"))
    data.setdefault("device_type", device.get("deviceType"))
    if ticket_code:
        data["ticket_code"] = ticket_code
    if warranty_code:
        data["warranty_code"] = warranty_code
    return IdentifiedEntities.model_validate(data)


async def coordinator_node(state: AgentState) -> dict:
    started = time.perf_counter()

    message = (state.get("user_message") or "").strip()
    issue = (state.get("issue_description") or "").strip()
    text_for_intent = f"{message} {issue}".strip() or message

    signals = classify_intent(text_for_intent)

    explicit_ticket = (state.get("ticket_code") or "").strip() or None
    explicit_warranty = (state.get("warranty_code") or "").strip() or None
    ticket_code = explicit_ticket or extract_ticket_code(state)
    warranty_code = explicit_warranty or extract_warranty_code(state)

    options = state.get("options") or {}
    allow_warranty = (
        bool(options.get("allowWarranty"))
        or bool(state.get("warranty_policies"))
        or bool(state.get("warranty_lookups"))
        or bool(explicit_warranty)
    )
    allow_tracking = (
        bool(options.get("allowTracking"))
        or bool(state.get("tracking_lookups"))
        or bool(explicit_ticket)
    )

    plan = build_plan(
        signals.intent,
        allow_diagnosis=bool(options.get("allowDiagnosis")),
        allow_quotation=bool(options.get("allowQuotation")),
        allow_appointment=bool(options.get("allowAppointment")),
        allow_warranty=allow_warranty,
        allow_tracking=allow_tracking,
    )

    elapsed_ms = int((time.perf_counter() - started) * 1000)
    timings = dict(state.get("timings") or {})
    timings["COORDINATOR"] = elapsed_ms

    logger.info(
        "coordinator request_id=%s intent=%s confidence=%.2f source=%s plan=%s",
        state.get("request_id"),
        signals.intent.value,
        signals.confidence,
        signals.source,
        plan,
    )

    return {
        "intent": signals.intent.value,
        "intent_confidence": signals.confidence,
        "secondary_intents": [s.value for s in signals.secondary],
        "plan": plan,
        "ticket_code": ticket_code,
        "warranty_code": warranty_code,
        "identified_entities": _initial_entities(state, ticket_code, warranty_code),
        "timings": timings,
    }


def intent_from_state(state: AgentState) -> Intent:
    try:
        return Intent(state.get("intent", Intent.UNKNOWN.value))
    except ValueError:
        return Intent.UNKNOWN
