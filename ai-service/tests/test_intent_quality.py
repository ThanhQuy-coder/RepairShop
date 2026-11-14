"""Hybrid 6-Intent classification regression, holdout accuracy & disambiguation tests."""

from __future__ import annotations

import json
from collections import Counter
from pathlib import Path

from app.ml.intent_classifier import (
    MIN_RULE_CONFIDENCE,
    classify_by_rules,
    classify_intent,
)
from app.schemas.agent_models import Intent

EVAL_CASES_PATH = Path(__file__).resolve().parent.parent / "data" / "eval_cases.json"
EVAL_HOLDOUT_PATH = Path(__file__).resolve().parent.parent / "data" / "eval_holdout.json"


def _cases() -> list[dict]:
    data = json.loads(EVAL_CASES_PATH.read_text(encoding="utf-8"))
    return data["cases"] if isinstance(data, dict) else data


def _holdout() -> list[dict]:
    data = json.loads(EVAL_HOLDOUT_PATH.read_text(encoding="utf-8"))
    return data["cases"] if isinstance(data, dict) else data


def _predict(case: dict) -> str:
    return classify_intent(case["message"]).intent.value


def test_eval_labels_cover_all_six_intents() -> None:
    labels = Counter(case["expectedIntent"] for case in _cases())
    assert set(labels) == {
        "DIAGNOSIS",
        "QUOTATION",
        "APPOINTMENT",
        "WARRANTY",
        "TRACKING",
        "UNKNOWN",
    }


def test_eval_cases_accuracy_is_high() -> None:
    cases = _cases()
    correct = sum(1 for case in cases if _predict(case) == case["expectedIntent"])
    accuracy = correct / len(cases)
    assert accuracy >= 0.90, f"Expected >= 90% on eval_cases, got {correct}/{len(cases)} ({accuracy:.2%})"


def test_eval_holdout_accuracy_across_six_classes() -> None:
    holdout = _holdout()
    assert len(holdout) == 90
    correct = sum(1 for case in holdout if _predict(case) == case["expectedIntent"])
    accuracy = correct / len(holdout)
    assert accuracy >= 0.85, f"Expected >= 85% on eval_holdout, got {correct}/{len(holdout)} ({accuracy:.2%})"


def test_warranty_eval_cases() -> None:
    case_by_id = {case["id"]: case for case in _cases()}
    for case_id in ("C25", "C26", "C27", "C28", "C29", "C30"):
        assert _predict(case_by_id[case_id]) == "WARRANTY"


def test_tricky_off_topic_words_still_classified_as_diagnosis() -> None:
    case_by_id = {case["id"]: case for case in _cases()}
    for case_id in ("C06", "C07", "C08"):
        assert _predict(case_by_id[case_id]) == "DIAGNOSIS"


def test_multi_intent_diagnosis_priority_and_secondary() -> None:
    signals = classify_intent("Pin chai rồi, cho tôi báo giá và đặt lịch mai 3h chiều")
    assert signals.intent == Intent.DIAGNOSIS
    assert Intent.QUOTATION in signals.secondary
    assert Intent.APPOINTMENT in signals.secondary


def test_generic_words_pin_gia_sua_do_not_trigger_confident_rule_false_positives() -> None:
    for generic in ("pin", "giá", "sửa"):
        rule = classify_by_rules(generic)
        assert rule.confidence < MIN_RULE_CONFIDENCE

    for off_topic in (
        "Giá vàng hôm nay bao nhiêu",
        "Sửa ống nước bị rò rỉ thế nào",
        "Pin con thỏ AA mua ở đâu",
    ):
        assert classify_intent(off_topic).intent == Intent.UNKNOWN


def test_warranty_and_tracking_code_regex_rules() -> None:
    w_sig = classify_by_rules("Kiểm tra giúp mã WR-2026-0001")
    assert w_sig.intent == Intent.WARRANTY
    assert w_sig.confidence >= MIN_RULE_CONFIDENCE

    t_sig = classify_by_rules("Tra cứu đơn RS-2026-0001")
    assert t_sig.intent == Intent.TRACKING
    assert t_sig.confidence >= MIN_RULE_CONFIDENCE
