"""
Đo so sánh MAS vs Single-Agent baseline (Task 4.3).

Chạy (từ thư mục ai-service):
    .\\venv\\Scripts\\python.exe scripts\\eval_compare.py --label ollama --sets dev,holdout

Kết quả:
- benchmarks/eval_rows_<label>.csv       (mỗi case × mỗi hệ thống 1 dòng, số liệu thô)
- benchmarks/eval_summary_<label>.json   (metric tổng hợp + cấu hình)
- In bảng Markdown để dán vào báo cáo.

Nguyên tắc công bằng:
- Cùng context (services/parts/slots), cùng options, cùng classifier intent (AD-06),
  cùng LLM, cùng timeout.
- Cả hai hệ thống đều bị hậu kiểm ID/giá/slot như nhau.
- Không dùng số liệu ước lượng; mọi metric sinh từ lần chạy thật.
"""

from __future__ import annotations

import argparse
import asyncio
import csv
import json
import statistics
import sys
import time
from datetime import date
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Dict, List, Optional

AI_SERVICE_DIR = Path(__file__).resolve().parents[1]
if str(AI_SERVICE_DIR) not in sys.path:
    sys.path.insert(0, str(AI_SERVICE_DIR))

DATA_DIR = AI_SERVICE_DIR / "data"
FIXED_TODAY = date(2026, 11, 9)
LABELS = ["DIAGNOSIS", "QUOTATION", "APPOINTMENT", "TRACKING", "UNKNOWN"]


def load_cases(set_name: str) -> List[dict]:
    path = DATA_DIR / ("eval_cases.json" if set_name == "dev" else "eval_holdout.json")
    return json.loads(path.read_text(encoding="utf-8"))["cases"]


def build_request(case: dict):
    from app.schemas.agent_models import AgentRequest
    from tests.fixtures.context import CORE_PARTS, CORE_SERVICES, CORE_SLOTS

    payload = {
        "requestId": f"11111111-1111-1111-1111-{case['id'].lower().replace('c', '').replace('h', '').zfill(12)[:12]}",
        "actorType": "GUEST",
        "message": case["message"],
        "device": case["device"],
        "issueDescription": case.get("issueDescription", ""),
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
    return AgentRequest.model_validate(payload)


def price_map() -> Dict[str, float]:
    from tests.fixtures.context import CORE_PARTS, CORE_SERVICES

    prices = {item["serviceId"]: float(item["basePrice"]) for item in CORE_SERVICES}
    prices.update({item["partId"]: float(item["unitPrice"]) for item in CORE_PARTS})
    return prices


def percentile(values: List[float], ratio: float) -> float:
    if not values:
        return 0.0
    ordered = sorted(values)
    if len(ordered) == 1:
        return ordered[0]
    position = (len(ordered) - 1) * ratio
    lower = int(position)
    upper = min(lower + 1, len(ordered) - 1)
    weight = position - lower
    return ordered[lower] * (1 - weight) + ordered[upper] * weight


async def run_case(system: str, request) -> Any:
    from app.baseline.single_agent import run_baseline
    from app.orchestration.graph import run_agents

    if system == "mas":
        return await run_agents(request)
    return await run_baseline(request)


def evaluate_response(
    system: str,
    set_name: str,
    case: dict,
    response,
    latency_ms: float,
    prices: Dict[str, float],
) -> dict:
    diagnosis = response.diagnosis
    diagnosis_ids = (
        [service.service_id for service in diagnosis.suggested_services]
        if diagnosis
        else []
    )
    quotation = response.quotation
    quoted_service_ids = [
        item.item_id for item in quotation.items if item.item_type == "SERVICE"
    ] if quotation else []
    # "Dịch vụ được đề xuất" = từ Diagnosis + từ Quotation (với câu hỏi giá, MAS không chạy
    # Diagnosis nên nếu chỉ đo diagnosis sẽ bất công cho MAS).
    recommended_ids = list(dict.fromkeys(diagnosis_ids + quoted_service_ids))

    expected = case.get("expectedServiceIds") or []
    rank: Optional[int] = None
    for index, service_id in enumerate(recommended_ids, start=1):
        if service_id in expected:
            rank = index
            break

    quotation_integrity = True
    if quotation and quotation.items:
        quotation_integrity = all(
            abs(item.unit_price - prices.get(item.item_id, -1)) < 0.01
            for item in quotation.items
        )
        quotation_integrity = quotation_integrity and abs(
            quotation.estimated_total - sum(item.line_total for item in quotation.items)
        ) < 0.01

    appointment = response.appointment
    slots_valid = True
    if appointment and appointment.suggested_slots:
        slots_valid = all(
            slot.available > 0 and not slot.is_full
            for slot in appointment.suggested_slots
        )

    return {
        "system": system,
        "set": set_name,
        "case_id": case["id"],
        "expected_intent": case["expectedIntent"],
        "predicted_intent": response.intent.value,
        "intent_ok": response.intent.value == case["expectedIntent"],
        "expected_services": "|".join(expected),
        "suggested_services": "|".join(diagnosis_ids),
        "recommended_services": "|".join(recommended_ids),
        "service_rank": rank or 0,
        "hit1": bool(rank == 1),
        "hit3": bool(rank is not None and rank <= 3),
        "quotation_items": len(quotation.items) if quotation else 0,
        "quotation_total": quotation.estimated_total if quotation else 0,
        "quotation_integrity": quotation_integrity,
        "slots_valid": slots_valid,
        "suggested_slots": len(appointment.suggested_slots) if appointment else 0,
        "status": response.status.value,
        "llm_calls": response.meta.llm_calls,
        "fallback": response.meta.fallback_used,
        "latency_ms": round(latency_ms, 1),
        "reason_length": len(response.reason),
    }


def metrics(rows: List[dict]) -> Dict[str, Any]:
    total = len(rows)
    quality = [row for row in rows if not row["fallback"]]
    n_quality = len(quality)

    intent_ok = sum(1 for row in quality if row["intent_ok"])
    intent_ok_all = sum(1 for row in rows if row["intent_ok"])

    service_rows = [row for row in quality if row["expected_services"]]
    hit1 = sum(1 for row in service_rows if row["hit1"]) / len(service_rows) if service_rows else 0
    hit3 = sum(1 for row in service_rows if row["hit3"]) / len(service_rows) if service_rows else 0
    mrr_values = [
        1 / row["service_rank"] for row in service_rows if row["service_rank"] > 0
    ]
    mrr = statistics.fmean(mrr_values) if mrr_values else 0

    quotation_rows = [row for row in quality if row["quotation_items"] > 0]
    quotation_integrity = (
        sum(1 for row in quotation_rows if row["quotation_integrity"]) / len(quotation_rows)
        if quotation_rows
        else 0
    )

    latencies = [row["latency_ms"] for row in rows]
    status_counts: Dict[str, int] = {}
    for row in rows:
        status_counts[row["status"]] = status_counts.get(row["status"], 0) + 1

    return {
        "n": total,
        "nQuality": n_quality,
        "fallbackRate": round(sum(1 for row in rows if row["fallback"]) / total, 4)
        if total
        else 0,
        "intentAccuracy": round(intent_ok / n_quality, 4) if n_quality else 0,
        "intentAccuracyAllRows": round(intent_ok_all / total, 4) if total else 0,
        "serviceHit1": round(hit1, 4),
        "serviceHit3": round(hit3, 4),
        "serviceMrr": round(mrr, 4),
        "serviceEvaluated": len(service_rows),
        "quotationIntegrity": round(quotation_integrity, 4),
        "quotationEvaluated": len(quotation_rows),
        "slotValidity": round(
            sum(1 for row in quality if row["slots_valid"]) / n_quality, 4
        )
        if n_quality
        else 0,
        "latencyMedianMs": round(statistics.median(latencies), 1) if latencies else 0,
        "latencyP95Ms": round(percentile(latencies, 0.95), 1),
        "latencyMinMs": round(min(latencies), 1) if latencies else 0,
        "latencyMaxMs": round(max(latencies), 1) if latencies else 0,
        "latencyLlmMedianMs": round(
            statistics.median([row["latency_ms"] for row in rows if row["llm_calls"] > 0]), 1
        )
        if any(row["llm_calls"] > 0 for row in rows)
        else 0,
        "latencyNonLlmMedianMs": round(
            statistics.median([row["latency_ms"] for row in rows if row["llm_calls"] == 0]), 1
        )
        if any(row["llm_calls"] == 0 for row in rows)
        else 0,
        "statusCounts": status_counts,
        "fallbackCount": sum(1 for row in rows if row["fallback"]),
        "avgLlmCalls": round(statistics.fmean([row["llm_calls"] for row in rows]), 3),
    }


async def main_async(label: str, sets: List[str], systems: List[str]) -> Dict[str, Any]:
    from app.agents.appointment import agent as appointment_module
    from app.agents.appointment.agent import AppointmentAgent
    from app.core.config import settings

    appointment_module._appointment_agent = AppointmentAgent(
        today_provider=lambda: FIXED_TODAY
    )
    prices = price_map()

    all_rows: List[dict] = []
    summary: Dict[str, Any] = {
        "label": label,
        "generatedAt": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "llm": {
            "mode": settings.llm_mode,
            "model": settings.llm_model,
            "baseUrlHost": settings.llm_base_url.split("://", 1)[-1].split("/", 1)[0],
        },
        "fixedToday": FIXED_TODAY.isoformat(),
        "results": {},
    }

    for set_name in sets:
        cases = load_cases(set_name)
        for system in systems:
            rows: List[dict] = []
            for case in cases:
                request = build_request(case)
                started = time.perf_counter()
                response = await run_case(system, request)
                latency_ms = (time.perf_counter() - started) * 1000
                rows.append(
                    evaluate_response(
                        system, set_name, case, response, latency_ms, prices
                    )
                )
            all_rows.extend(rows)
            summary["results"][f"{set_name}:{system}"] = metrics(rows)

    return {"summary": summary, "rows": all_rows}


def _fmt(value: float) -> str:
    return f"{value:.3f}"


def main() -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")

    parser = argparse.ArgumentParser()
    parser.add_argument("--label", default="ollama")
    parser.add_argument("--sets", default="dev,holdout")
    parser.add_argument("--systems", default="mas,baseline")
    args = parser.parse_args()

    sets = [item.strip() for item in args.sets.split(",") if item.strip()]
    systems = [item.strip() for item in args.systems.split(",") if item.strip()]

    output = asyncio.run(main_async(args.label, sets, systems))
    summary = output["summary"]
    rows = output["rows"]

    benchmark_dir = AI_SERVICE_DIR / "benchmarks"
    benchmark_dir.mkdir(parents=True, exist_ok=True)

    csv_path = benchmark_dir / f"eval_rows_{args.label}.csv"
    with csv_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(rows[0].keys()))
        writer.writeheader()
        writer.writerows(rows)

    summary_path = benchmark_dir / f"eval_summary_{args.label}.json"
    summary_path.write_text(
        json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8"
    )

    print(f"[{args.label}] model={summary['llm']['model']} sets={sets} systems={systems}")
    header = (
        "| Set | System | n | nQuality | Intent Acc | Intent(all) | Hit@1 | Hit@3 | MRR | "
        "Price Integrity | Slot Validity | Median ms | p95 ms | Fallback | LLM calls |"
    )
    print(header)
    print("|" + "---|" * 15)
    for set_name in sets:
        for system in systems:
            result = summary["results"][f"{set_name}:{system}"]
            print(
                f"| {set_name} | {system} | {result['n']} | {result['nQuality']} | "
                f"{_fmt(result['intentAccuracy'])} | {_fmt(result['intentAccuracyAllRows'])} | "
                f"{_fmt(result['serviceHit1'])} | {_fmt(result['serviceHit3'])} | "
                f"{_fmt(result['serviceMrr'])} | {_fmt(result['quotationIntegrity'])} | "
                f"{_fmt(result['slotValidity'])} | {result['latencyMedianMs']} | "
                f"{result['latencyP95Ms']} | {_fmt(result['fallbackRate'])} | "
                f"{result['avgLlmCalls']} |"
            )

    print(f"saved: {csv_path.relative_to(AI_SERVICE_DIR)}")
    print(f"saved: {summary_path.relative_to(AI_SERVICE_DIR)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
