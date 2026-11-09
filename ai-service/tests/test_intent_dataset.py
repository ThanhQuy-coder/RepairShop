"""Task 2.1/2.3 — Kiểm tra tính toàn vẹn của dataset intent."""

from __future__ import annotations

import csv
from collections import Counter
from pathlib import Path

DATA_PATH = Path(__file__).resolve().parents[1] / "data" / "intent_dataset.csv"
EXPECTED_LABELS = {"DIAGNOSIS", "QUOTATION", "APPOINTMENT", "TRACKING", "UNKNOWN"}


def _rows() -> list[dict]:
    with DATA_PATH.open(encoding="utf-8", newline="") as handle:
        return list(csv.DictReader(handle))


def test_dataset_has_at_least_150_sentences():
    assert len(_rows()) >= 150


def test_dataset_has_exactly_five_intent_labels():
    labels = {row["label"] for row in _rows()}
    assert labels == EXPECTED_LABELS


def test_every_label_has_at_least_20_examples():
    counts = Counter(row["label"] for row in _rows())
    for label in EXPECTED_LABELS:
        assert counts[label] >= 20, f"{label} chỉ có {counts[label]} mẫu"


def test_no_duplicate_or_blank_sentences():
    texts = [row["text"].strip() for row in _rows()]
    assert all(texts)
    assert len(texts) == len(set(texts))


def test_hard_cases_are_present_for_off_topic_guard():
    texts = {row["text"].lower() for row in _rows()}
    assert any("error code 404" in text for text in texts)
    assert any("thanh toán" in text for text in texts)
    assert any("qr code" in text for text in texts)
