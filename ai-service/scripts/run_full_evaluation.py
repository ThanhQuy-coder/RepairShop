"""
Full System Evaluation Runner (TASK-15).

Chạy thực nghiệm toàn diện trên hệ thống AI Hybrid của RepairShop (chế độ deterministic/offline,
không gọi LLM ngoài, dùng dữ liệu thực `intent_metrics.json`, `eval_cases.json`, `eval_holdout.json`
và bộ ma trận kiểm thử Service Matching / Part Compatibility / Functional Pipeline 6 bước).

Kết quả lưu ra:
- `ai-service/app/ml/models/full_evaluation_metrics.json`

Chạy:
    $env:INTERNAL_API_KEY="test-key-123"
    $env:PYTHONPATH="ai-service;ai-service\\venv\\Lib\\site-packages"
    C:\\Apps\\python.exe ai-service\\scripts\\run_full_evaluation.py
"""

from __future__ import annotations

import asyncio
import json
import statistics
import sys
import time
import uuid
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Dict, List

AI_SERVICE_DIR = Path(__file__).resolve().parents[1]
if str(AI_SERVICE_DIR) not in sys.path:
    sys.path.insert(0, str(AI_SERVICE_DIR))

from app.core.config import settings
from app.ml.intent_classifier import classify_intent
from app.orchestration.pipeline import run_pipeline

LABELS = ["DIAGNOSIS", "QUOTATION", "APPOINTMENT", "WARRANTY", "TRACKING", "UNKNOWN"]
from app.retrieval.retrieval_service import (
    is_vague_symptom_description,
    retrieve_candidates,
)
from app.schemas.agent_models import AgentRequest, AgentStatus
from app.schemas.request_models import (
    AdvisoryContext,
    AvailablePart,
    AvailableService,
    DeviceInfo,
    DeviceType,
)
from tests.fixtures.context import CORE_PARTS, CORE_SERVICES, CORE_SLOTS

DATA_DIR = AI_SERVICE_DIR / "data"
MODELS_DIR = AI_SERVICE_DIR / "app" / "ml" / "models"
INTENT_METRICS_PATH = MODELS_DIR / "intent_metrics.json"
OUTPUT_PATH = MODELS_DIR / "full_evaluation_metrics.json"


def _percentile(values: List[float], ratio: float) -> float:
    if not values:
        return 0.0
    ordered = sorted(values)
    if len(ordered) == 1:
        return round(ordered[0], 4)
    pos = (len(ordered) - 1) * ratio
    lo = int(pos)
    hi = min(lo + 1, len(ordered) - 1)
    w = pos - lo
    return round(ordered[lo] * (1.0 - w) + ordered[hi] * w, 4)


def _latency_stats(samples_ms: List[float]) -> Dict[str, float]:
    if not samples_ms:
        return {"count": 0, "meanMs": 0.0, "p50Ms": 0.0, "p95Ms": 0.0, "maxMs": 0.0}
    return {
        "count": len(samples_ms),
        "meanMs": round(statistics.mean(samples_ms), 4),
        "p50Ms": _percentile(samples_ms, 0.50),
        "p95Ms": _percentile(samples_ms, 0.95),
        "maxMs": round(max(samples_ms), 4),
    }


def _macro_f1(y_true: List[str], y_pred: List[str], labels: List[str]) -> float:
    f1s: List[float] = []
    for label in labels:
        tp = sum(1 for t, p in zip(y_true, y_pred) if t == label and p == label)
        fp = sum(1 for t, p in zip(y_true, y_pred) if t != label and p == label)
        fn = sum(1 for t, p in zip(y_true, y_pred) if t == label and p != label)
        prec = tp / (tp + fp) if (tp + fp) else 0.0
        rec = tp / (tp + fn) if (tp + fn) else 0.0
        f1 = (2 * prec * rec / (prec + rec)) if (prec + rec) else 0.0
        if any(t == label for t in y_true) or any(p == label for p in y_pred):
            f1s.append(f1)
    return round(sum(f1s) / len(f1s), 4) if f1s else 0.0


def evaluate_intent_live(cases: List[Dict[str, Any]]) -> Dict[str, Any]:
    """Đo live `classify_intent` trên tập đánh giá độc lập (sau 1 lần warm-up)."""
    if cases:
        classify_intent(cases[0]["message"])

    y_true: List[str] = []
    y_pred: List[str] = []
    latencies_ms: List[float] = []
    mismatches: List[Dict[str, str]] = []

    for case in cases:
        t0 = time.perf_counter()
        sig = classify_intent(case["message"])
        latencies_ms.append((time.perf_counter() - t0) * 1000.0)
        expected = case["expectedIntent"]
        predicted = sig.intent.value
        y_true.append(expected)
        y_pred.append(predicted)
        if predicted != expected:
            mismatches.append({"id": case["id"], "expected": expected, "predicted": predicted, "message": case["message"]})

    acc = round(sum(1 for t, p in zip(y_true, y_pred) if t == p) / len(cases), 4) if cases else 0.0
    return {
        "sampleCount": len(cases),
        "accuracy": acc,
        "macroF1": _macro_f1(y_true, y_pred, LABELS),
        "latencyMs": _latency_stats(latencies_ms),
        "mismatches": mismatches,
    }


def _rank_of_expected(ranked_ids: List[str], expected_ids: List[str]) -> Optional[int]:
    for idx, sid in enumerate(ranked_ids, start=1):
        if sid in expected_ids:
            return idx
    return None


def evaluate_service_matching(eval_cases: List[Dict[str, Any]], eval_holdout: List[Dict[str, Any]]) -> Dict[str, Any]:
    """Đo Hit@1, Hit@3, MRR và phát hiện câu mơ hồ / no-match."""
    context = AdvisoryContext(
        available_services=[AvailableService.model_validate(s) for s in CORE_SERVICES],
        available_parts=[AvailablePart.model_validate(p) for p in CORE_PARTS],
    )

    combined = eval_cases + eval_holdout
    positive_eval = [c for c in combined if c.get("expectedServiceIds")]

    def _run_batch(batch: List[Dict[str, Any]], remap_legacy_laptop_cleaning: bool = False) -> Dict[str, Any]:
        hit1 = 0
        hit3 = 0
        rr_sum = 0.0
        matched_any_count = 0
        latencies: List[float] = []
        mismatches: List[Dict[str, Any]] = []

        for case in batch:
            dev_raw = case["device"]
            dev = DeviceInfo(
                deviceType=DeviceType(dev_raw["deviceType"].lower()),
                brand=dev_raw["brand"],
                model=dev_raw["model"],
            )
            issue = case.get("issueDescription") or case["message"]
            expected = list(case["expectedServiceIds"])
            if remap_legacy_laptop_cleaning and dev.device_type == DeviceType.LAPTOP and expected == ["svc-ve-sinh"]:
                expected = ["svc-tan-nhiet"]

            t0 = time.perf_counter()
            res = retrieve_candidates(dev, issue, context)
            latencies.append((time.perf_counter() - t0) * 1000.0)

            matched_ids = [s.service_id for s in res.candidate_services if res.service_scores.get(s.service_id, 0) > 0]
            if res.matched_any_service:
                matched_any_count += 1
            rank = _rank_of_expected(matched_ids, expected)
            if rank == 1:
                hit1 += 1
            if rank is not None and rank <= 3:
                hit3 += 1
            if rank is not None:
                rr_sum += 1.0 / rank
            else:
                mismatches.append({
                    "id": case["id"],
                    "deviceType": dev.device_type.value,
                    "issue": issue,
                    "expected": expected,
                    "top3Predicted": matched_ids[:3],
                })
            if rank != 1 and rank is not None:
                mismatches.append({
                    "id": case["id"],
                    "deviceType": dev.device_type.value,
                    "issue": issue,
                    "expected": expected,
                    "top3Predicted": matched_ids[:3],
                    "rank": rank,
                })

        n = len(batch)
        return {
            "sampleCount": n,
            "hitAt1": round(hit1 / n, 4) if n else 0.0,
            "hitAt3": round(hit3 / n, 4) if n else 0.0,
            "mrr": round(rr_sum / n, 4) if n else 0.0,
            "matchedAnyRate": round(matched_any_count / n, 4) if n else 0.0,
            "latencyMs": _latency_stats(latencies),
            "nonTop1Details": mismatches,
        }

    # Curated 20-case Service Matching & Vague/No-Match suite
    curated_positives = [
        {"id": "SM01", "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"}, "issueDescription": "Pin chai tụt nhanh", "expectedServiceIds": ["svc-pin"]},
        {"id": "SM02", "device": {"deviceType": "phone", "brand": "Samsung", "model": "S23"}, "issueDescription": "pin tut nhanh ko dau", "expectedServiceIds": ["svc-pin"]},
        {"id": "SM03", "device": {"deviceType": "phone", "brand": "iPhone", "model": "14"}, "issueDescription": "vo man hinh chay muc", "expectedServiceIds": ["svc-man-hinh"]},
        {"id": "SM04", "device": {"deviceType": "phone", "brand": "Xiaomi", "model": "Redmi"}, "issueDescription": "Cảm ứng bị liệt, loạn cảm ứng", "expectedServiceIds": ["svc-cam-ung"]},
        {"id": "SM05", "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"}, "issueDescription": "Sạc không vào điện, lỏng chân sạc", "expectedServiceIds": ["svc-sac"]},
        {"id": "SM06", "device": {"deviceType": "phone", "brand": "Oppo", "model": "Reno 8"}, "issueDescription": "Loa rè, nghe nhỏ xíu", "expectedServiceIds": ["svc-loa"]},
        {"id": "SM07", "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"}, "issueDescription": "Camera sau chụp mờ, không lấy nét", "expectedServiceIds": ["svc-camera"]},
        {"id": "SM08", "device": {"deviceType": "phone", "brand": "iPhone", "model": "12"}, "issueDescription": "Máy rơi nước cần vệ sinh main", "expectedServiceIds": ["svc-ve-sinh"]},
        {"id": "SM09", "device": {"deviceType": "laptop", "brand": "Dell", "model": "Inspiron 15"}, "issueDescription": "Bàn phím kẹt phím, gõ nhảy chữ", "expectedServiceIds": ["svc-ban-phim"]},
        {"id": "SM10", "device": {"deviceType": "laptop", "brand": "Asus", "model": "Vivobook"}, "issueDescription": "Pin laptop phồng, tụt nhanh", "expectedServiceIds": ["svc-pin-laptop"]},
        {"id": "SM11", "device": {"deviceType": "laptop", "brand": "HP", "model": "Pavilion"}, "issueDescription": "Quạt kêu to, máy nóng ran cần vệ sinh tản nhiệt", "expectedServiceIds": ["svc-tan-nhiet"]},
        {"id": "SM12", "device": {"deviceType": "laptop", "brand": "Lenovo", "model": "ThinkPad"}, "issueDescription": "ban phim laptop liet nut backspace", "expectedServiceIds": ["svc-ban-phim"]},
        {"id": "SM13", "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"}, "issueDescription": "loa bi re ko nghe ro", "expectedServiceIds": ["svc-loa"]},
        {"id": "SM14", "device": {"deviceType": "phone", "brand": "Samsung", "model": "A54"}, "issueDescription": "cam mo ko chup dc", "expectedServiceIds": ["svc-camera"]},
        {"id": "SM15", "device": {"deviceType": "laptop", "brand": "Dell", "model": "Latitude"}, "issueDescription": "pin lap chai sac ko day", "expectedServiceIds": ["svc-pin-laptop"]},
        {"id": "SM16", "device": {"deviceType": "phone", "brand": "iPhone", "model": "15"}, "issueDescription": "thay pin ip 15 nhanh het", "expectedServiceIds": ["svc-pin"]},
    ]
    curated_negatives = [
        {"id": "SN01", "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"}, "issueDescription": "Máy bị hư cần kiểm tra"},
        {"id": "SN02", "device": {"deviceType": "phone", "brand": "Samsung", "model": "S23"}, "issueDescription": "Điện thoại bị lỗi rồi shop"},
        {"id": "SN03", "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"}, "issueDescription": "Lỗi error code 404 khi mở app"},
        {"id": "SN04", "device": {"deviceType": "laptop", "brand": "Dell", "model": "Inspiron 15"}, "issueDescription": "Thiết bị có vấn đề bất thường cần xem giúp"},
    ]

    neg_correct = 0
    neg_latencies: List[float] = []
    for neg in curated_negatives:
        dev = DeviceInfo(
            deviceType=DeviceType(neg["device"]["deviceType"]),
            brand=neg["device"]["brand"],
            model=neg["device"]["model"],
        )
        t0 = time.perf_counter()
        res = retrieve_candidates(dev, neg["issueDescription"], context)
        neg_latencies.append((time.perf_counter() - t0) * 1000.0)
        is_rejected = (not res.matched_any_service) or is_vague_symptom_description(neg["issueDescription"])
        if is_rejected:
            neg_correct += 1

    return {
        "evalCombinedRaw": _run_batch(positive_eval, remap_legacy_laptop_cleaning=False),
        "evalCombinedDeviceNormalized": _run_batch(positive_eval, remap_legacy_laptop_cleaning=True),
        "curatedPositiveSuite": _run_batch(curated_positives, remap_legacy_laptop_cleaning=False),
        "curatedVagueAndNoMatchSuite": {
            "sampleCount": len(curated_negatives),
            "correctlyFlagged": neg_correct,
            "rejectionAccuracy": round(neg_correct / len(curated_negatives), 4),
            "falsePositiveRate": round((len(curated_negatives) - neg_correct) / len(curated_negatives), 4),
            "latencyMs": _latency_stats(neg_latencies),
        },
        "legacyLabelNote": "eval_holdout.json cases H02 and H09 tag laptop thermal cleaning queries with legacy phone service ID svc-ve-sinh instead of laptop service ID svc-tan-nhiet; DeviceType Layer-1 intentionally filters out phone services for laptop devices.",
    }


def evaluate_part_compatibility() -> Dict[str, Any]:
    """Đánh giá ma trận lọc linh kiện 3 lớp (DeviceType, Category/Symptom, Brand/Model) & lọc hết hàng."""
    parts_catalog = [
        AvailablePart(partId="p-pin-ip13", name="Pin iPhone 13", unitPrice=350000, compatibleDeviceType="phone", inStock=True, category="Pin", quantityOnHand=5),
        AvailablePart(partId="p-sac-ip-gen", name="Cáp sạc iPhone", unitPrice=180000, compatibleDeviceType="phone", inStock=True, category="Sạc", quantityOnHand=10),
        AvailablePart(partId="p-pin-ss-s24", name="Pin Samsung Galaxy S24", unitPrice=420000, compatibleDeviceType="phone", inStock=True, category="Pin", quantityOnHand=4),
        AvailablePart(partId="p-mh-ip14pro", name="Màn hình iPhone 14 Pro", unitPrice=3500000, compatibleDeviceType="phone", inStock=True, category="Màn hình", quantityOnHand=2),
        AvailablePart(partId="p-mh-ss-s24", name="Màn hình Samsung Galaxy S24", unitPrice=2900000, compatibleDeviceType="phone", inStock=True, category="Màn hình", quantityOnHand=3),
        AvailablePart(partId="p-cam-ip13", name="Cụm camera iPhone 13", unitPrice=950000, compatibleDeviceType="phone", inStock=True, category="Camera", quantityOnHand=2),
        AvailablePart(partId="p-loa-ip13", name="Loa trong iPhone 13", unitPrice=220000, compatibleDeviceType="phone", inStock=True, category="Loa", quantityOnHand=3),
        AvailablePart(partId="p-pin-xiaomi", name="Pin Xiaomi Redmi Note 12", unitPrice=310000, compatibleDeviceType="phone", inStock=True, category="Pin", quantityOnHand=4),
        AvailablePart(partId="p-pin-dell-ins15", name="Pin laptop Dell Inspiron 15", unitPrice=650000, compatibleDeviceType="laptop", inStock=True, category="Pin laptop", quantityOnHand=3),
        AvailablePart(partId="p-bp-dell-ins15", name="Bàn phím Dell Inspiron 15", unitPrice=480000, compatibleDeviceType="laptop", inStock=True, category="Bàn phím", quantityOnHand=2),
        AvailablePart(partId="p-pin-dell-xps13", name="Pin laptop Dell XPS 13", unitPrice=990000, compatibleDeviceType="laptop", inStock=True, category="Pin laptop", quantityOnHand=2),
        AvailablePart(partId="p-bp-macbook", name="Bàn phím MacBook Pro M2", unitPrice=1450000, compatibleDeviceType="laptop", inStock=True, category="Bàn phím", quantityOnHand=2),
        # Out-of-stock parts for stock-gating verification
        AvailablePart(partId="p-mh-ip13-oos", name="Màn hình iPhone 13", unitPrice=1200000, compatibleDeviceType="phone", inStock=False, category="Màn hình", quantityOnHand=0),
        AvailablePart(partId="p-sac-ip13-zeroqty", name="Cụm chân sạc iPhone 13", unitPrice=280000, compatibleDeviceType="phone", inStock=True, category="Sạc", quantityOnHand=0),
    ]
    ctx = AdvisoryContext(
        available_services=[AvailableService.model_validate(s) for s in CORE_SERVICES],
        available_parts=parts_catalog,
    )

    test_cases = [
        # (deviceType, brand, model, issue, target_part_id, expected_compatible_in_stock, category)
        ("phone", "iPhone", "13", "Pin tụt nhanh", "p-pin-ip13", True, "valid_exact_model"),
        ("phone", "iPhone", "13", "Sạc không vào", "p-sac-ip-gen", True, "valid_generic_brand"),
        ("phone", "Samsung", "Galaxy S24", "Bể màn hình", "p-mh-ss-s24", True, "valid_exact_model"),
        ("laptop", "Dell", "Inspiron 15", "Pin laptop chai", "p-pin-dell-ins15", True, "valid_exact_model"),
        ("laptop", "Dell", "Inspiron 15", "Liệt bàn phím", "p-bp-dell-ins15", True, "valid_exact_model"),
        ("phone", "iPhone", "13", "Camera chụp mờ", "p-cam-ip13", True, "valid_exact_model"),
        ("phone", "iPhone", "13", "Loa rè", "p-loa-ip13", True, "valid_exact_model"),
        ("phone", "Samsung", "Galaxy S24", "Pin chai", "p-pin-ss-s24", True, "valid_exact_model"),
        # Negative Layer 1 (DeviceType mismatch)
        ("phone", "iPhone", "13", "Pin tụt nhanh", "p-pin-dell-ins15", False, "layer1_cross_device"),
        ("laptop", "Dell", "Inspiron 15", "Màn hình sọc", "p-mh-ss-s24", False, "layer1_cross_device"),
        ("phone", "iPhone", "13", "Liệt phím", "p-bp-dell-ins15", False, "layer1_cross_device"),
        # Negative Layer 2 (Category/Symptom mismatch)
        ("phone", "iPhone", "13", "Pin tụt nhanh", "p-cam-ip13", False, "layer2_symptom_mismatch"),
        ("phone", "iPhone", "13", "Camera chụp mờ", "p-pin-ip13", False, "layer2_symptom_mismatch"),
        ("phone", "Samsung", "Galaxy S24", "Loa nhỏ", "p-mh-ss-s24", False, "layer2_symptom_mismatch"),
        # Negative Layer 3 (Cross-brand / Cross-model mismatch)
        ("phone", "iPhone", "13", "Pin tụt nhanh", "p-pin-ss-s24", False, "layer3_cross_brand"),
        ("phone", "iPhone", "13", "Pin tụt nhanh", "p-pin-xiaomi", False, "layer3_cross_brand"),
        ("phone", "iPhone", "13", "Vỡ màn hình", "p-mh-ip14pro", False, "layer3_cross_model"),
        ("laptop", "Dell", "Inspiron 15", "Pin tụt nhanh", "p-pin-dell-xps13", False, "layer3_cross_model"),
        ("laptop", "Dell", "Inspiron 15", "Hỏng bàn phím", "p-bp-macbook", False, "layer3_cross_brand"),
        ("phone", "Samsung", "Galaxy S24", "Vỡ màn hình", "p-mh-ip14pro", False, "layer3_cross_brand"),
    ]

    tp = fp = tn = fn = 0
    latencies: List[float] = []
    errors: List[Dict[str, Any]] = []
    for dev_type, brand, model, issue, target_part, expected, tag in test_cases:
        dev = DeviceInfo(deviceType=DeviceType(dev_type), brand=brand, model=model)
        t0 = time.perf_counter()
        res = retrieve_candidates(dev, issue, ctx)
        latencies.append((time.perf_counter() - t0) * 1000.0)
        returned_ids = {p.part_id for p in res.candidate_parts if res.part_scores.get(p.part_id, 0) > 0}
        predicted = target_part in returned_ids
        if expected and predicted:
            tp += 1
        elif not expected and predicted:
            fp += 1
            errors.append({"tag": tag, "part": target_part, "brand": brand, "model": model, "issue": issue, "type": "FP"})
        elif not expected and not predicted:
            tn += 1
        else:
            fn += 1
            errors.append({"tag": tag, "part": target_part, "brand": brand, "model": model, "issue": issue, "type": "FN"})

    # Stock gating check
    oos_checks = [
        ("phone", "iPhone", "13", "Vỡ màn hình", "p-mh-ip13-oos"),
        ("phone", "iPhone", "13", "Hỏng chân sạc", "p-sac-ip13-zeroqty"),
    ]
    oos_excluded = 0
    oos_surfaced = 0
    for dev_type, brand, model, issue, target_oos in oos_checks:
        res = retrieve_candidates(DeviceInfo(deviceType=DeviceType(dev_type), brand=brand, model=model), issue, ctx)
        if target_oos not in {p.part_id for p in res.candidate_parts}:
            oos_excluded += 1
        if target_oos in {p.part_id for p in res.out_of_stock_parts}:
            oos_surfaced += 1

    precision = round(tp / (tp + fp), 4) if (tp + fp) else 0.0
    recall = round(tp / (tp + fn), 4) if (tp + fn) else 0.0
    f1 = round((2 * precision * recall) / (precision + recall), 4) if (precision + recall) else 0.0
    fpr = round(fp / (fp + tn), 4) if (fp + tn) else 0.0
    return {
        "totalPairs": len(test_cases),
        "tp": tp,
        "fp": fp,
        "tn": tn,
        "fn": fn,
        "precision": precision,
        "recall": recall,
        "f1": f1,
        "falsePositiveRate": fpr,
        "outOfStockChecks": len(oos_checks),
        "outOfStockExcludedFromCandidates": oos_excluded,
        "outOfStockTrackedSeparately": oos_surfaced,
        "outOfStockExclusionRate": round(oos_excluded / len(oos_checks), 4),
        "latencyMs": _latency_stats(latencies),
        "errors": errors,
    }


def _build_pipeline_request(
    case: Dict[str, Any],
    *,
    actor_type: str = "GUEST",
    ticket_code: Optional[str] = None,
    warranty_code: Optional[str] = None,
    include_customer_lookups: bool = False,
    allow_diagnosis: bool = True,
    allow_quotation: bool = True,
    allow_appointment: bool = True,
    allow_warranty: bool = True,
    allow_tracking: bool = True,
) -> AgentRequest:
    ctx_warranty_policies = [
        {
            "packageId": "pkg-std-6m",
            "name": "Gói bảo hành Tiêu chuẩn 6 tháng",
            "durationMonths": 6,
            "terms": "Bảo hành lỗi kỹ thuật linh kiện thay thế trong 6 tháng; không áp dụng rơi vỡ vào nước.",
            "isActive": True,
        }
    ]
    ctx_warranties = (
        [
            {
                "warrantyCode": "BH-20261109-001",
                "ticketCode": "RS-20261109-001",
                "deviceLabel": "iPhone 13",
                "startDate": "2026-10-01",
                "endDate": "2027-04-01",
                "status": "Active",
                "isExpired": False,
                "packageName": "Gói bảo hành Tiêu chuẩn 6 tháng",
            }
        ]
        if (include_customer_lookups or warranty_code == "BH-20261109-001")
        else []
    )
    ctx_tracking = (
        [
            {
                "ticketCode": "RS-20261109-001",
                "deviceLabel": "iPhone 13",
                "status": "InRepair",
                "statusLabel": "Đang sửa chữa",
                "statusHistory": [
                    {
                        "status": "CheckedIn",
                        "statusLabel": "Đã tiếp nhận",
                        "changedAt": "2026-11-09T08:00:00+07:00",
                    }
                ],
            }
        ]
        if (include_customer_lookups or ticket_code == "RS-20261109-001")
        else []
    )

    payload = {
        "requestId": str(uuid.uuid4()),
        "actorType": actor_type,
        "message": case["message"],
        "device": case["device"],
        "issueDescription": case.get("issueDescription", ""),
        "context": {
            "availableServices": CORE_SERVICES,
            "availableParts": CORE_PARTS,
            "availableSlots": CORE_SLOTS,
            "warrantyPolicies": ctx_warranty_policies,
            "warrantyLookups": ctx_warranties,
            "trackingLookups": ctx_tracking,
        },
        "options": {
            "allowDiagnosis": allow_diagnosis,
            "allowQuotation": allow_quotation,
            "allowAppointment": allow_appointment,
            "allowWarranty": allow_warranty,
            "allowTracking": allow_tracking,
        },
        "customerInfo": {"fullName": None, "phone": None},
        "referenceDate": "2026-11-09",
        "ticketCode": ticket_code,
        "warrantyCode": warranty_code,
    }
    return AgentRequest.model_validate(payload)


async def evaluate_pipeline(eval_cases: List[Dict[str, Any]], eval_holdout: List[Dict[str, Any]]) -> Dict[str, Any]:
    """Chạy toàn bộ 120 case (eval_cases + eval_holdout) và bộ kịch bản 6 Intent qua `run_pipeline` (offline)."""
    original_mode = settings.llm_mode
    settings.llm_mode = "offline"
    try:
        all_eval = eval_cases + eval_holdout
        # Warm-up 1 request
        await run_pipeline(_build_pipeline_request(all_eval[0]))

        svc_prices = {s["serviceId"]: float(s["basePrice"]) for s in CORE_SERVICES}
        part_prices = {p["partId"]: float(p["unitPrice"]) for p in CORE_PARTS}
        valid_service_ids = set(svc_prices)
        valid_part_ids = set(part_prices)
        valid_item_prices = {**svc_prices, **part_prices}
        valid_slots = {s["timeSlotId"]: s for s in CORE_SLOTS}
        valid_pkg_ids = {"pkg-std-6m"}

        status_counts: Counter[str] = Counter()
        intent_hits = 0
        non_error = 0
        price_valid = 0
        slot_valid = 0
        id_valid = 0
        latencies: List[float] = []

        for case in all_eval:
            req = _build_pipeline_request(case)
            t0 = time.perf_counter()
            resp = await run_pipeline(req)
            latencies.append((time.perf_counter() - t0) * 1000.0)

            status_counts[resp.status.value] += 1
            if resp.status != AgentStatus.ERROR:
                non_error += 1
            if resp.intent.value == case["expectedIntent"]:
                intent_hits += 1

            # Check grounded IDs
            grounded_ok = True
            if resp.diagnosis:
                if any(s.service_id not in valid_service_ids for s in resp.diagnosis.suggested_services):
                    grounded_ok = False
                if any(p.part_id not in valid_part_ids for p in resp.diagnosis.suggested_parts):
                    grounded_ok = False
            if resp.warranty:
                if any(p.package_id not in valid_pkg_ids for p in resp.warranty.policies):
                    grounded_ok = False
                if resp.warranty.warranties:
                    grounded_ok = False  # guest without explicit code must never leak warranties
            if resp.tracking and resp.tracking.tickets:
                grounded_ok = False  # guest without explicit code must never leak tickets
            if grounded_ok:
                id_valid += 1

            # Check quotation price integrity
            q_ok = True
            if resp.quotation:
                expected_total = 0.0
                for item in resp.quotation.items:
                    ctx_price = valid_item_prices.get(item.item_id)
                    if ctx_price is None or abs(item.unit_price - ctx_price) > 1e-6:
                        q_ok = False
                    expected_total += item.line_total
                if abs(resp.quotation.estimated_total - round(expected_total, 2)) > 1e-2:
                    q_ok = False
            if q_ok:
                price_valid += 1

            # Check slot integrity
            s_ok = True
            if resp.appointment:
                for sl in resp.appointment.suggested_slots:
                    raw = valid_slots.get(sl.time_slot_id)
                    if raw is None or int(raw["available"]) <= 0 or bool(raw["isFull"]):
                        s_ok = False
                if resp.appointment.booking_draft and resp.appointment.booking_draft.time_slot_id:
                    if resp.appointment.booking_draft.time_slot_id not in valid_slots:
                        s_ok = False
            if s_ok:
                slot_valid += 1

        n = len(all_eval)

        # Dedicated 12-scenario 6-Intent + Privacy + Edge suite
        scenarios = [
            ("SC01_DIAGNOSIS_CLEAR", _build_pipeline_request({"message": "iPhone 13 pin tụt nhanh", "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"}, "issueDescription": "Pin tụt nhanh"}), {"DIAGNOSIS"}, {"SUCCESS", "PARTIAL"}),
            ("SC02_QUOTATION_EXACT", _build_pipeline_request({"message": "Giá thay pin iPhone 13 bao nhiêu?", "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"}, "issueDescription": ""}), {"QUOTATION"}, {"SUCCESS"}),
            ("SC03_APPOINTMENT_AVAILABLE", _build_pipeline_request({"message": "Đặt lịch sửa máy lúc 9h sáng ngày 10/11/2026", "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"}, "issueDescription": ""}), {"APPOINTMENT"}, {"SUCCESS"}),
            ("SC04_WARRANTY_POLICY", _build_pipeline_request({"message": "Chính sách bảo hành bên cửa hàng thế nào?", "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"}, "issueDescription": ""}), {"WARRANTY"}, {"SUCCESS", "PARTIAL"}),
            ("SC05_WARRANTY_LOOKUP_CODE", _build_pipeline_request({"message": "Kiểm tra bảo hành mã BH-20261109-001", "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"}, "issueDescription": ""}, warranty_code="BH-20261109-001"), {"WARRANTY"}, {"SUCCESS"}),
            ("SC06_TRACKING_LOOKUP_CODE", _build_pipeline_request({"message": "Tra cứu tiến độ phiếu RS-20261109-001", "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"}, "issueDescription": ""}, ticket_code="RS-20261109-001"), {"TRACKING"}, {"SUCCESS"}),
            ("SC07_GUEST_NO_TRACK_CODE", _build_pipeline_request({"message": "Kiểm tra tiến độ đơn sửa của tôi", "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"}, "issueDescription": ""}), {"TRACKING"}, {"NEED_CLARIFICATION"}),
            ("SC08_CUSTOMER_AUTH_TRACK", _build_pipeline_request({"message": "Đơn sửa máy của tôi tới đâu rồi?", "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"}, "issueDescription": ""}, actor_type="CUSTOMER", include_customer_lookups=True), {"TRACKING"}, {"SUCCESS"}),
            ("SC09_CUSTOMER_AUTH_WARRANTY", _build_pipeline_request({"message": "Kiểm tra bảo hành máy của tôi giúp", "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"}, "issueDescription": ""}, actor_type="CUSTOMER", include_customer_lookups=True), {"WARRANTY"}, {"SUCCESS"}),
            ("SC10_MULTI_INTENT", _build_pipeline_request({"message": "iPhone 13 chai pin, báo giá và đặt lịch lúc 9h sáng 10/11", "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"}, "issueDescription": "Pin tụt nhanh"}), {"DIAGNOSIS", "QUOTATION", "APPOINTMENT"}, {"SUCCESS"}),
            ("SC11_VAGUE_NEED_CLARIFY", _build_pipeline_request({"message": "Máy bị hư cần kiểm tra", "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"}, "issueDescription": "Máy bị hư cần kiểm tra"}, allow_quotation=False, allow_appointment=False), {"DIAGNOSIS"}, {"NEED_CLARIFICATION", "NO_MATCH"}),
            ("SC12_OUT_OF_SCOPE", _build_pipeline_request({"message": "Thời tiết Hà Nội hôm nay bao nhiêu độ?", "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"}, "issueDescription": ""}), {"UNKNOWN"}, {"SUCCESS", "OUT_OF_SCOPE"}),
        ]
        sc_results: List[Dict[str, Any]] = []
        sc_pass = 0
        for name, req, exp_intents, exp_statuses in scenarios:
            resp = await run_pipeline(req)
            ok = (resp.intent.value in exp_intents) and (resp.status.value in exp_statuses)
            if ok:
                sc_pass += 1
            sc_results.append({
                "scenario": name,
                "predictedIntent": resp.intent.value,
                "status": resp.status.value,
                "executedAgents": [a.value for a in resp.executed_agents],
                "passed": ok,
            })

        return {
            "evalBatchSize": n,
            "intentMatchRate": round(intent_hits / n, 4),
            "nonErrorRate": round(non_error / n, 4),
            "statusDistribution": dict(status_counts),
            "priceIntegrityRate": round(price_valid / n, 4),
            "slotIntegrityRate": round(slot_valid / n, 4),
            "groundedIdRate": round(id_valid / n, 4),
            "latencyMs": _latency_stats(latencies),
            "scenarioSuiteCount": len(scenarios),
            "scenarioSuitePassed": sc_pass,
            "scenarioSuitePassRate": round(sc_pass / len(scenarios), 4),
            "scenarioDetails": sc_results,
        }
    finally:
        settings.llm_mode = original_mode


async def main() -> None:
    eval_cases = json.loads((DATA_DIR / "eval_cases.json").read_text(encoding="utf-8"))["cases"]
    eval_holdout = json.loads((DATA_DIR / "eval_holdout.json").read_text(encoding="utf-8"))["cases"]
    intent_metrics = json.loads(INTENT_METRICS_PATH.read_text(encoding="utf-8"))

    live_intent = evaluate_intent_live(eval_cases + eval_holdout)
    service_matching = evaluate_service_matching(eval_cases, eval_holdout)
    part_compat = evaluate_part_compatibility()
    e2e_pipeline = await evaluate_pipeline(eval_cases, eval_holdout)

    output = {
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
        "intentClassification": {
            "artifactSummary": intent_metrics,
            "liveEvalCombined120": live_intent,
        },
        "serviceMatching": service_matching,
        "partCompatibility": part_compat,
        "endToEndPipeline": e2e_pipeline,
    }
    OUTPUT_PATH.write_text(json.dumps(output, ensure_ascii=False, indent=2), encoding="utf-8")

    print("=== FULL EVALUATION SUMMARY ===")
    print(f"Intent Live (n=120): acc={live_intent['accuracy']:.4f}, macroF1={live_intent['macroF1']:.4f}, p95={live_intent['latencyMs']['p95Ms']} ms")
    print(f"Service Matching Eval Raw (n={service_matching['evalCombinedRaw']['sampleCount']}): Hit@1={service_matching['evalCombinedRaw']['hitAt1']:.4f}, Hit@3={service_matching['evalCombinedRaw']['hitAt3']:.4f}, MRR={service_matching['evalCombinedRaw']['mrr']:.4f}")
    print(f"Service Matching Eval Normalized (n={service_matching['evalCombinedDeviceNormalized']['sampleCount']}): Hit@1={service_matching['evalCombinedDeviceNormalized']['hitAt1']:.4f}, Hit@3={service_matching['evalCombinedDeviceNormalized']['hitAt3']:.4f}, MRR={service_matching['evalCombinedDeviceNormalized']['mrr']:.4f}")
    print(f"Service Matching Curated Pos (n={service_matching['curatedPositiveSuite']['sampleCount']}): Hit@1={service_matching['curatedPositiveSuite']['hitAt1']:.4f}, Hit@3={service_matching['curatedPositiveSuite']['hitAt3']:.4f}, MRR={service_matching['curatedPositiveSuite']['mrr']:.4f}")
    print(f"Service Matching Vague/NoMatch (n={service_matching['curatedVagueAndNoMatchSuite']['sampleCount']}): rejAcc={service_matching['curatedVagueAndNoMatchSuite']['rejectionAccuracy']:.4f}")
    print(f"Part Compatibility (n={part_compat['totalPairs']}): P={part_compat['precision']:.4f}, R={part_compat['recall']:.4f}, F1={part_compat['f1']:.4f}, FPR={part_compat['falsePositiveRate']:.4f}, OOS_Excl={part_compat['outOfStockExclusionRate']:.4f}")
    print(f"E2E Pipeline (n=120, offline): nonErr={e2e_pipeline['nonErrorRate']:.4f}, priceOK={e2e_pipeline['priceIntegrityRate']:.4f}, slotOK={e2e_pipeline['slotIntegrityRate']:.4f}, idOK={e2e_pipeline['groundedIdRate']:.4f}, mean={e2e_pipeline['latencyMs']['meanMs']}ms, p95={e2e_pipeline['latencyMs']['p95Ms']}ms")
    print(f"E2E 12-Scenario 6-Intent Suite: {e2e_pipeline['scenarioSuitePassed']}/{e2e_pipeline['scenarioSuiteCount']} passed")
    print(f"Saved: {OUTPUT_PATH}")


if __name__ == "__main__":
    asyncio.run(main())
