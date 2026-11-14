"""Kiểm tra tính toàn vẹn, phân bố nhãn và chống rò rỉ dữ liệu của dataset intent (TASK-03)."""

from __future__ import annotations

import csv
import json
import re
import unicodedata
from collections import Counter
from pathlib import Path

DATA_DIR = Path(__file__).resolve().parents[1] / "data"
DATA_PATH = DATA_DIR / "intent_dataset.csv"
EVAL_CASES_PATH = DATA_DIR / "eval_cases.json"
EVAL_HOLDOUT_PATH = DATA_DIR / "eval_holdout.json"
DIAGNOSIS_LABELS_PATH = DATA_DIR / "diagnosis_labels.csv"

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


def _rows() -> list[dict]:
    with DATA_PATH.open(encoding="utf-8", newline="") as handle:
        return list(csv.DictReader(handle))


def test_dataset_in_target_range_500_to_800():
    rows = _rows()
    assert 500 <= len(rows) <= 800


def test_dataset_has_all_six_intent_labels():
    labels = {row["label"] for row in _rows()}
    assert labels == EXPECTED_LABELS


def test_every_label_is_well_represented():
    counts = Counter(row["label"] for row in _rows())
    for label in EXPECTED_LABELS:
        assert counts[label] >= 80, f"{label} chỉ có {counts[label]} mẫu"


def test_no_duplicate_blank_or_invalid_source_rows():
    rows = _rows()
    raw_texts = [row["text"].strip() for row in rows]
    norm_texts = [_normalize(row["text"]) for row in rows]
    sources = {row["source"].strip() for row in rows}
    assert all(raw_texts)
    assert len(raw_texts) == len(set(raw_texts))
    assert len(norm_texts) == len(set(norm_texts))
    assert sources.issubset(ALLOWED_SOURCES)


def test_zero_exact_leakage_against_eval_and_holdout():
    train_norm = {_normalize(row["text"]) for row in _rows()}
    eval_cases = json.loads(EVAL_CASES_PATH.read_text(encoding="utf-8"))["cases"]
    holdout_cases = json.loads(EVAL_HOLDOUT_PATH.read_text(encoding="utf-8"))["cases"]

    for case in eval_cases:
        assert _normalize(case["message"]) not in train_norm, f"Rò rỉ eval_cases {case['id']}"
    for case in holdout_cases:
        assert _normalize(case["message"]) not in train_norm, f"Rò rỉ eval_holdout {case['id']}"

    assert len(eval_cases) == 30
    assert len(holdout_cases) == 90
    holdout_counts = Counter(c["expectedIntent"] for c in holdout_cases)
    assert all(holdout_counts[label] == 15 for label in EXPECTED_LABELS)


def test_diagnosis_labels_has_no_duplicate_phrases():
    with DIAGNOSIS_LABELS_PATH.open(encoding="utf-8", newline="") as handle:
        rows = list(csv.DictReader(handle))
    phrases = [_normalize(row["text"]) for row in rows]
    assert len(phrases) == len(set(phrases))


def test_hard_cases_are_present_for_off_topic_guard():
    texts = {row["text"].lower() for row in _rows()}
    assert any("error code 404" in text for text in texts)
    assert any("thanh toán" in text for text in texts)
    assert any("qr code" in text for text in texts)
