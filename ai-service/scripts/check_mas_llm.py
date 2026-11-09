"""
Kiểm tra MAS end-to-end với LLM THẬT (Ollama local) — Task 2.2.

Chạy (từ thư mục ai-service):
    # Ollama (model nhỏ, chạy local):
    $env:LLM_MODE="ollama"; $env:LLM_BASE_URL="http://127.0.0.1:11434/v1";
    $env:LLM_MODEL="qwen2.5:1.5b-instruct-q4_K_M"; $env:LLM_API_KEY="ollama"
    .\\venv\\Scripts\\python.exe scripts\\check_mas_llm.py --label ollama --runs 3

    # Ollama local (mặc định của AI Service):
    .\\venv\\Scripts\\python.exe scripts\\check_mas_llm.py --label ollama --runs 3

Kết quả ghi ra `benchmarks/mas_real_llm_<label>.json` — KHÔNG dùng số này làm metric chất lượng,
chỉ để chứng minh pipeline chạy với LLM thật và đo độ trễ.
"""

from __future__ import annotations

import argparse
import asyncio
import json
import statistics
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

AI_SERVICE_DIR = Path(__file__).resolve().parents[1]
if str(AI_SERVICE_DIR) not in sys.path:
    sys.path.insert(0, str(AI_SERVICE_DIR))

CASES = [
    {
        "id": "R1",
        "message": "iPhone 13 của tôi pin tụt nhanh, máy nóng khi sạc",
        "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"},
        "issueDescription": "Pin tụt nhanh, máy nóng khi sạc",
    },
    {
        "id": "R2",
        "message": "Laptop Dell bị nóng, quạt kêu to bất thường",
        "device": {"deviceType": "laptop", "brand": "Dell", "model": "Inspiron 15"},
        "issueDescription": "Máy nóng, quạt kêu to",
    },
]


def build_payload(case: dict) -> dict:
    from tests.fixtures.context import CORE_PARTS, CORE_SERVICES

    return {
        "requestId": "b24f61c0-3f75-4c1e-b19e-4d5d2c7f22e1",
        "actorType": "GUEST",
        "message": case["message"],
        "device": case["device"],
        "issueDescription": case["issueDescription"],
        "context": {
            "availableServices": CORE_SERVICES,
            "availableParts": CORE_PARTS,
            "availableSlots": [],
        },
        "options": {
            "allowDiagnosis": True,
            "allowQuotation": False,
            "allowAppointment": False,
        },
    }


async def run_once(case: dict):
    from app.orchestration.graph import run_agents
    from app.schemas.agent_models import AgentRequest

    request = AgentRequest.model_validate(build_payload(case))
    started = time.perf_counter()
    response = await run_agents(request)
    elapsed_ms = round((time.perf_counter() - started) * 1000, 1)

    diagnosis = response.diagnosis
    return {
        "caseId": case["id"],
        "status": response.status.value,
        "intent": response.intent.value,
        "executedAgents": [agent.value for agent in response.executed_agents],
        "latencyMs": elapsed_ms,
        "llmCalls": response.meta.llm_calls,
        "fallbackUsed": response.meta.fallback_used,
        "serviceIds": [s.service_id for s in diagnosis.suggested_services] if diagnosis else [],
        "partIds": [p.part_id for p in diagnosis.suggested_parts] if diagnosis else [],
        "causeCount": len(diagnosis.possible_causes) if diagnosis else 0,
        "explanationLength": len(diagnosis.explanation) if diagnosis else 0,
        "reason": response.reason,
    }


async def main_async(label: str, runs: int) -> dict:
    from app.core.config import settings

    cold_started = time.perf_counter()
    import app.orchestration.graph  # noqa: F401  (đo cold start graph)

    cold_start_ms = round((time.perf_counter() - cold_started) * 1000, 1)

    results = []
    for _ in range(runs):
        for case in CASES:
            results.append(await run_once(case))

    latencies = [item["latencyMs"] for item in results]
    return {
        "label": label,
        "generatedAt": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "llm": {
            "mode": settings.llm_mode,
            "baseUrlHost": settings.llm_base_url.split("://", 1)[-1].split("/", 1)[0],
            "model": settings.llm_model,
        },
        "coldStartGraphImportMs": cold_start_ms,
        "runs": runs,
        "cases": results,
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
            "llmCallCounts": {
                str(calls): sum(1 for item in results if item["llmCalls"] == calls)
                for calls in {item["llmCalls"] for item in results}
            },
        },
    }


def main() -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")

    parser = argparse.ArgumentParser()
    parser.add_argument("--label", default="ollama")
    parser.add_argument("--runs", type=int, default=2)
    parser.add_argument("--out", type=Path, default=None)
    args = parser.parse_args()

    result = asyncio.run(main_async(args.label, args.runs))

    output = args.out or AI_SERVICE_DIR / "benchmarks" / f"mas_real_llm_{args.label}.json"
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")

    print(f"[{args.label}] model={result['llm']['model']} mode={result['llm']['mode']}")
    print(f"cold start graph import: {result['coldStartGraphImportMs']} ms")
    for item in result["cases"]:
        print(
            f"  {item['caseId']} status={item['status']:9s} {item['latencyMs']:8.1f} ms "
            f"llmCalls={item['llmCalls']} fallback={item['fallbackUsed']} "
            f"services={item['serviceIds']} causes={item['causeCount']}"
        )
    print(f"summary: {result['summary']}")
    print(f"saved: {output.relative_to(AI_SERVICE_DIR)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
