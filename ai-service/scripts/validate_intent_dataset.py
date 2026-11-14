"""
Script kiểm tra chất lượng và chống rò rỉ dữ liệu cho tập Intent Classification (TASK-03).

Chạy từ root hoặc thư mục ai-service:
    python scripts/validate_intent_dataset.py
"""

from __future__ import annotations

import csv
import json
import re
import unicodedata
from collections import Counter
from pathlib import Path
from typing import Dict, List, Set, Tuple

AI_SERVICE_DIR = Path(__file__).resolve().parents[1]
DATASET_PATH = AI_SERVICE_DIR / "data" / "intent_dataset.csv"
EVAL_CASES_PATH = AI_SERVICE_DIR / "data" / "eval_cases.json"
EVAL_HOLDOUT_PATH = AI_SERVICE_DIR / "data" / "eval_holdout.json"

EXPECTED_LABELS = {
    "DIAGNOSIS",
    "QUOTATION",
    "APPOINTMENT",
    "WARRANTY",
    "TRACKING",
    "UNKNOWN",
}
ALLOWED_SOURCES = {
    "manual_curated",
    "natural_variant_reviewed",
    "typo_no_accent_reviewed",
    "multi_intent_reviewed",
    "out_of_scope_reviewed",
}


def _normalize(text: str) -> str:
    lowered = unicodedata.normalize("NFC", text.lower())
    return re.sub(r"\s+", " ", lowered).strip()


def _tokens(text: str) -> Set[str]:
    return set(re.findall(r"[0-9a-zà-ỹđ]+", _normalize(text)))


def _jaccard(a: Set[str], b: Set[str]) -> float:
    if not a or not b:
        return 0.0
    union = a | b
    return len(a & b) / len(union)


def _has_diacritics(text: str) -> bool:
    decomposed = unicodedata.normalize("NFD", text)
    return any(unicodedata.category(ch) == "Mn" for ch in decomposed)


def validate_dataset() -> Tuple[bool, Dict[str, object]]:
    rows = list(csv.DictReader(DATASET_PATH.open(encoding="utf-8", newline="")))
    fieldnames = csv.DictReader(DATASET_PATH.open(encoding="utf-8", newline="")).fieldnames

    errors: List[str] = []
    if fieldnames != ["text", "label", "source"]:
        errors.append(f"Header CSV không chuẩn: {fieldnames}")

    if not (500 <= len(rows) <= 800):
        errors.append(f"Số lượng mẫu ({len(rows)}) không nằm trong khoảng 500–800.")

    seen_raw: Dict[str, int] = {}
    seen_norm: Dict[str, int] = {}
    label_counts: Counter = Counter()
    source_counts: Counter = Counter()
    train_items: List[Tuple[int, str, str, Set[str]]] = []
    diacritic_counts: Counter = Counter()

    for idx, row in enumerate(rows, start=2):
        text = (row.get("text") or "").strip()
        label = (row.get("label") or "").strip()
        source = (row.get("source") or "").strip()

        if not text:
            errors.append(f"Dòng {idx}: text rỗng.")
            continue
        if label not in EXPECTED_LABELS:
            errors.append(f"Dòng {idx}: label không hợp lệ '{label}'.")
        if source not in ALLOWED_SOURCES:
            errors.append(f"Dòng {idx}: source không hợp lệ '{source}'.")

        norm = _normalize(text)
        if text in seen_raw:
            errors.append(f"Dòng {idx}: trùng lặp nguyên văn với dòng {seen_raw[text]}.")
        seen_raw[text] = idx
        if norm in seen_norm:
            errors.append(f"Dòng {idx}: trùng lặp chuẩn hóa với dòng {seen_norm[norm]}.")
        seen_norm[norm] = idx

        label_counts[label] += 1
        source_counts[source] += 1
        diacritic_counts["accented" if _has_diacritics(text) else "unaccented"] += 1
        train_items.append((idx, norm, label, _tokens(text)))

    for label in sorted(EXPECTED_LABELS):
        if label_counts[label] < 80:
            errors.append(f"Nhãn '{label}' chỉ có {label_counts[label]} mẫu (< 80).")

    eval_cases = json.loads(EVAL_CASES_PATH.read_text(encoding="utf-8"))["cases"]
    eval_holdout = json.loads(EVAL_HOLDOUT_PATH.read_text(encoding="utf-8"))["cases"]

    exact_leak_eval: List[str] = []
    exact_leak_holdout: List[str] = []
    near_leak_warnings: List[dict] = []

    train_norm_set = {item[1] for item in train_items}
    for case in eval_cases:
        norm = _normalize(case["message"])
        if norm in train_norm_set:
            exact_leak_eval.append(case["id"])
        toks = _tokens(case["message"])
        if len(toks) >= 5:
            for row_idx, _, _, tr_toks in train_items:
                if len(tr_toks) >= 5:
                    sim = _jaccard(toks, tr_toks)
                    if sim >= 0.85:
                        near_leak_warnings.append(
                            {"split": "eval_cases", "id": case["id"], "trainRow": row_idx, "jaccard": round(sim, 3)}
                        )

    for case in eval_holdout:
        norm = _normalize(case["message"])
        if norm in train_norm_set:
            exact_leak_holdout.append(case["id"])
        toks = _tokens(case["message"])
        if len(toks) >= 5:
            for row_idx, _, _, tr_toks in train_items:
                if len(tr_toks) >= 5:
                    sim = _jaccard(toks, tr_toks)
                    if sim >= 0.85:
                        near_leak_warnings.append(
                            {"split": "eval_holdout", "id": case["id"], "trainRow": row_idx, "jaccard": round(sim, 3)}
                        )

    if exact_leak_eval:
        errors.append(f"Rò rỉ nguyên văn sang eval_cases.json: {exact_leak_eval}")
    if exact_leak_holdout:
        errors.append(f"Rò rỉ nguyên văn sang eval_holdout.json: {exact_leak_holdout}")
    if near_leak_warnings:
        errors.append(f"Phát hiện câu gần giống (>=0.85 Jaccard) giữa train và eval: {near_leak_warnings}")

    lengths_char = [len((row.get("text") or "").strip()) for row in rows if (row.get("text") or "").strip()]
    lengths_word = [len((row.get("text") or "").strip().split()) for row in rows if (row.get("text") or "").strip()]

    summary = {
        "totalTrainSamples": len(rows),
        "evalCasesCount": len(eval_cases),
        "evalHoldoutCount": len(eval_holdout),
        "labelDistribution": dict(sorted(label_counts.items())),
        "sourceDistribution": dict(sorted(source_counts.items())),
        "diacriticDistribution": dict(sorted(diacritic_counts.items())),
        "charLength": {
            "min": min(lengths_char) if lengths_char else 0,
            "max": max(lengths_char) if lengths_char else 0,
            "avg": round(sum(lengths_char) / len(lengths_char), 2) if lengths_char else 0.0,
        },
        "wordLength": {
            "min": min(lengths_word) if lengths_word else 0,
            "max": max(lengths_word) if lengths_word else 0,
            "avg": round(sum(lengths_word) / len(lengths_word), 2) if lengths_word else 0.0,
        },
        "exactLeakEval": exact_leak_eval,
        "exactLeakHoldout": exact_leak_holdout,
        "nearLeakCount": len(near_leak_warnings),
        "errors": errors,
        "passed": len(errors) == 0,
    }
    return len(errors) == 0, summary


def main() -> int:
    passed, summary = validate_dataset()
    print(json.dumps(summary, ensure_ascii=False, indent=2))
    return 0 if passed else 1


if __name__ == "__main__":
    raise SystemExit(main())
