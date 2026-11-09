"""
Kiểm tra end-to-end qua Backend thật (Task 4.1/4.4): tư vấn → đặt lịch → huỷ.

Yêu cầu: Backend đang chạy (mặc định http://localhost:5252), PostgreSQL có dữ liệu seed,
AI Service đang chạy (http://localhost:8000).

Chạy:
    .\\venv\\Scripts\\python.exe scripts\\check_e2e_backend.py --base http://localhost:5252
"""

from __future__ import annotations

import argparse
import json
import sys
import time
from pathlib import Path

import httpx

AI_SERVICE_DIR = Path(__file__).resolve().parents[1]

CONSULT_PAYLOAD = {
    "deviceType": "Phone",
    "brand": "iPhone",
    "model": "13",
    "issueDescription": "Pin tụt nhanh, máy nóng khi sạc. Muốn báo giá và đặt lịch.",
    "allowQuotation": True,
    "allowAppointment": True,
}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--base", default="http://localhost:5252")
    parser.add_argument("--phone", default="0900000001")
    args = parser.parse_args()

    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")

    out: dict = {}
    with httpx.Client(base_url=args.base, timeout=120) as client:
        started = time.perf_counter()
        consult = client.post("/api/ai/consult", json=CONSULT_PAYLOAD)
        out["consult"] = {
            "http": consult.status_code,
            "latencyMs": round((time.perf_counter() - started) * 1000, 1),
            "body": consult.json() if consult.status_code == 200 else consult.text[:300],
        }
        if consult.status_code != 200:
            print("consult failed:", out["consult"]["http"], out["consult"]["body"])
            return 1

        body = consult.json()
        slots = (body.get("appointment") or {}).get("suggestedSlots") or []
        if not slots:
            print("consult ok but no slots; booking step skipped")
            out["booking"] = None
        else:
            slot = slots[0]
            booking_payload = {
                "fullName": "Khách Kiểm Thử",
                "phone": args.phone,
                "deviceInfo": {
                    "deviceType": "Phone",
                    "brand": "iPhone",
                    "model": "13",
                    "issueDescription": CONSULT_PAYLOAD["issueDescription"],
                },
                "appointmentDate": slot["appointmentDate"],
                "timeSlotId": slot["timeSlotId"],
            }
            started = time.perf_counter()
            created = client.post("/api/appointments", json=booking_payload)
            created_body = created.json() if created.status_code < 400 else {}
            out["booking"] = {
                "http": created.status_code,
                "latencyMs": round((time.perf_counter() - started) * 1000, 1),
                "id": created_body.get("id"),
                "status": created_body.get("status"),
                "hasLookupToken": bool(created_body.get("lookupToken")),
                "date": created_body.get("appointmentDate"),
                "slotId": slot["timeSlotId"],
            }

            if created.status_code < 400 and created_body.get("lookupToken"):
                cancelled = client.patch(
                    f"/api/appointments/{created_body['id']}/cancel",
                    json={"lookupToken": created_body["lookupToken"], "phone": args.phone},
                )
                cancel_body = cancelled.json() if cancelled.status_code < 400 else {}
                out["cancel"] = {
                    "http": cancelled.status_code,
                    "status": cancel_body.get("status"),
                }

    output = AI_SERVICE_DIR / "benchmarks" / "e2e_booking_http.json"
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(out, ensure_ascii=False, indent=2), encoding="utf-8")

    print(json.dumps(out, ensure_ascii=False, indent=2))
    print("saved:", output.relative_to(AI_SERVICE_DIR))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
