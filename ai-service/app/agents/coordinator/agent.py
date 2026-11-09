"""Coordinator Agent (Task 2.1) — phân loại intent và lập kế hoạch chạy agent.

Coordinator KHÔNG gọi LLM: giữ ngân sách 1 LLM call/request cho Diagnosis
(xem HUONG_DAN_TRIEN_KHAI_MAS_4_NGAY.md mục 4.3.4).
"""

from __future__ import annotations

import logging
import time

from app.ml.intent_classifier import classify_intent
from app.orchestration.routing import build_plan
from app.schemas.agent_models import Intent
from app.schemas.state_models import AgentState

logger = logging.getLogger(__name__)


async def coordinator_node(state: AgentState) -> dict:
    started = time.perf_counter()

    message = (state.get("user_message") or "").strip()
    issue = (state.get("issue_description") or "").strip()
    text_for_intent = f"{message} {issue}".strip() or message

    signals = classify_intent(text_for_intent)

    options = state.get("options") or {}
    plan = build_plan(
        signals.intent,
        allow_diagnosis=bool(options.get("allowDiagnosis")),
        allow_quotation=bool(options.get("allowQuotation")),
        allow_appointment=bool(options.get("allowAppointment")),
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
        "plan": plan,
        "timings": timings,
    }


def intent_from_state(state: AgentState) -> Intent:
    try:
        return Intent(state.get("intent", Intent.UNKNOWN.value))
    except ValueError:
        return Intent.UNKNOWN
