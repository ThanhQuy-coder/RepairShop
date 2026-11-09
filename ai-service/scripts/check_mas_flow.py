"""
Đo luồng MAS đầy đủ (Diagnosis → Quotation → Appointment) với LLM THẬT — Task 3.3.

Chạy (từ thư mục ai-service):
    .\\venv\\Scripts\\python.exe scripts\\check_mas_flow.py --label ollama --runs 3
    # Ollama: set LLM_BASE_URL/LLM_MODEL/LLM_API_KEY như scripts/check_mas_llm.py

Kết quả: benchmarks/mas_flow_<label>.json
"""

from __future__ import annotations

import argparse
import asyncio
import json
import statistics
import sys
import time
from datetime import date
from datetime import datetime, timezone
from pathlib import Path

AI_SERVICE_DIR = Path(__file__).resolve().parents[1]
if str(AI_SERVICE_DIR) not in sys.path:
    sys.path.insert(0, str(AI_SERVICE_DIR))

FIXED_TODAY = date(2026, 11, 9)
MESSAGE = "Pin chai rồi, báo giá và đặt lịch mai 3h chiều"
ISSUE = "Pin chai, máy nóng khi sạc"


def build_payload() -> dict:
    from tests.fixtures.context import CORE_PARTS, CORE_SERVICES, CORE_SLOTS

    return {
        "requestId": "b24f61c0-3f75-4c1e-b19e-4d5d2c7f22e1",
        "actorType": "GUEST",
        "message": MESSAGE,
        "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"},
        "issueDescription": ISSUE,
        "context": {
            "availableServices": CORE_SERVICES,
            "availableParts": CORE_PARTS,
            "availableSlots": CORE_SLOTS,
        },
        "options": {
            "allowDiagnosis": True,
            "allowQuotation": True,
            "allowAppointment": True,
        },
        "customerInfo": {"fullName": None, "phone": None},
    }


async def run_once():
    from app.orchestration.graph import run_agents
    from app.schemas.agent_models import AgentRequest

    request = AgentRequest.model_validate(build_payload())
    started = time.perf_counter()
    response = await run_agents(request)
    elapsed_ms = round((time.perf_counter() - started) * 1000, 1)

    diagnosis = response.diagnosis
    quotation = response.quotation
    appointment = response.appointment

    return {
        "status": response.status.value,
        "intent": response.intent.value,
        "executedAgents": [agent.value for agent in response.executed_agents],
        "latencyMs": elapsed_ms,
        "llmCalls": response.meta.llm_calls,
        "fallbackUsed": response.meta.fallback_used,
        "services": [s.service_id for s in diagnosis.suggested_services] if diagnosis else [],
        "parts": [p.part_id for p in diagnosis.suggested_parts] if diagnosis else [],
        "quotationTotal": quotation.estimated_total if quotation else None,
        "quotationItems": len(quotation.items) if quotation else 0,
        "suggestedSlots": [s.time_slot_id for s in appointment.suggested_slots]
        if appointment
        else [],
        "bookingDraft": {
            "date": appointment.booking_draft.appointment_date,
            "slot": appointment.booking_draft.time_slot_id,
            "missing": appointment.missing_information,
        }
        if appointment and appointment.booking_draft
        else None,
        "reason": response.reason,
    }


async def main_async(label: str, runs: int) -> dict:
    from app.agents.appointment import agent as appointment_module
    from app.agents.appointment.agent import AppointmentAgent
    from app.core.config import settings

    # Cố định "hôm nay" để "mai" luôn trỏ 2026-11-10 (khớp slot fixture).
    appointment_module._appointment_agent = AppointmentAgent(
        today_provider=lambda: FIXED_TODAY
    )

    results = [await run_once() for _ in range(runs)]
    latencies = [item["latencyMs"] for item in results]

    return {
        "label": label,
        "generatedAt": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "llm": {
            "mode": settings.llm_mode,
            "baseUrlHost": settings.llm_base_url.split("://", 1)[-1].split("/", 1)[0],
            "model": settings.llm_model,
        },
        "fixedToday": FIXED_TODAY.isoformat(),
        "message": MESSAGE,
        "runs": results,
        "summary": {
            "n": len(latencies),
            "medianMs": round(statistics.median(latencies), 1),
            "minMs": round(min(latencies), 1),
            "maxMs": round(max(latencies), 1),
            "statusCounts": {
                status: sum(1 for item in results if item["status"] == status)
                for status in {item["status"] for item in results}
            },
            "fallbackCount": sum(1 for item in results if item["fallbackUsed"]),
        },
    }


def main() -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")

    parser = argparse.ArgumentParser()
    parser.add_argument("--label", default="ollama")
    parser.add_argument("--runs", type=int, default=3)
    args = parser.parse_args()

    result = asyncio.run(main_async(args.label, args.runs))
    output = AI_SERVICE_DIR / "benchmarks" / f"mas_flow_{args.label}.json"
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")

    print(f"[{args.label}] model={result['llm']['model']} runs={len(result['runs'])}")
    for item in result["runs"]:
        print(
            f"  {item['status']:9s} {item['latencyMs']:8.1f} ms llm={item['llmCalls']} "
            f"agents={item['executedAgents']} quote={item['quotationTotal']} "
            f"slots={item['suggestedSlots']} slotDraft={(item['bookingDraft'] or {}).get('slot')}"
        )
    print(f"summary: {result['summary']}")
    print(f"saved: {output.relative_to(AI_SERVICE_DIR)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
