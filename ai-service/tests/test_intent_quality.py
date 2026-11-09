"""Task 2.3 — Chất lượng phân loại intent trên bộ eval ĐỘC LẬP (không dùng để train).

Nguồn nhãn: `data/eval_cases.json` do nhóm tự gán tay, tách khỏi `data/intent_dataset.csv`.
Đây là con số trung thực để báo cáo (không phải accuracy trên tập train).
"""

from __future__ import annotations

import json
from collections import Counter
from pathlib import Path

from app.ml.intent_classifier import classify_intent

DATA_DIR = Path(__file__).resolve().parents[1] / "data"
EVAL_PATH = DATA_DIR / "eval_cases.json"
MODEL_DIR = Path(__file__).resolve().parents[1] / "app" / "ml" / "models"


def _cases() -> list[dict]:
    return json.loads(EVAL_PATH.read_text(encoding="utf-8"))["cases"]


def _predict(case: dict) -> str:
    text = f"{case['message']} {case.get('issueDescription', '')}".strip()
    return classify_intent(text).intent.value


def test_eval_set_has_at_least_20_labelled_cases():
    assert len(_cases()) >= 20


def test_intent_accuracy_on_independent_eval_set():
    cases = _cases()
    predictions = [(case, _predict(case)) for case in cases]
    correct = sum(1 for case, predicted in predictions if predicted == case["expectedIntent"])
    accuracy = correct / len(cases)

    print(f"\nIntent accuracy trên eval độc lập: {accuracy:.3f} ({correct}/{len(cases)})")
    print("Các case dự đoán sai (nếu có):")
    for case, predicted in predictions:
        if predicted != case["expectedIntent"]:
            print(f"  - {case['id']}: {case['expectedIntent']} -> {predicted} | {case['message']}")

    assert accuracy >= 0.85, f"Accuracy {accuracy:.3f} thấp hơn ngưỡng 0.85"


def test_tricky_off_topic_words_still_classified_as_diagnosis():
    case_by_id = {case["id"]: case for case in _cases()}
    for case_id in ("C06", "C07", "C08"):
        assert _predict(case_by_id[case_id]) == "DIAGNOSIS"


def test_pure_unrelated_messages_are_unknown():
    case_by_id = {case["id"]: case for case in _cases()}
    for case_id in ("C16", "C17", "C18"):
        assert _predict(case_by_id[case_id]) == "UNKNOWN"


def test_eval_labels_cover_all_five_intents():
    labels = Counter(case["expectedIntent"] for case in _cases())
    assert set(labels) == {
        "DIAGNOSIS",
        "QUOTATION",
        "APPOINTMENT",
        "TRACKING",
        "UNKNOWN",
    }


def test_intent_metrics_file_matches_dataset_and_model():
    metrics = json.loads((MODEL_DIR / "intent_metrics.json").read_text(encoding="utf-8"))
    dataset_size = sum(
        1
        for line in (DATA_DIR / "intent_dataset.csv").read_text(encoding="utf-8").splitlines()[1:]
        if line.strip()
    )

    assert metrics["datasetSize"] == dataset_size
    assert metrics["holdout"]["size"] >= 20
    assert metrics["holdout"]["macroF1"] >= 0.8
    assert metrics["crossValidation5Fold"]["mean"] >= 0.8
    assert (MODEL_DIR / "intent_clf.joblib").is_file()
