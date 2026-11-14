"""Train and benchmark 6-class Intent Classifiers (Rule-based vs ML vs Hybrid).

Saves:
- app/ml/models/intent_clf.joblib
- app/ml/models/intent_metrics.json (full benchmarks, confusion matrices, latencies)
"""

from __future__ import annotations

import csv
import json
import sys
import time
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

import joblib
import numpy as np
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.linear_model import LogisticRegression
from sklearn.metrics import (
    accuracy_score,
    classification_report,
    confusion_matrix,
    f1_score,
)
from sklearn.model_selection import StratifiedKFold, cross_val_score, train_test_split
from sklearn.pipeline import Pipeline

from app.ml.intent_classifier import (
    MODEL_PATH,
    MlIntentClassifier,
    classify_by_rules,
    classify_intent,
    normalize_text,
)

DATASET_PATH = ROOT / "data" / "intent_dataset.csv"
EVAL_CASES_PATH = ROOT / "data" / "eval_cases.json"
EVAL_HOLDOUT_PATH = ROOT / "data" / "eval_holdout.json"
MODEL_DIR = ROOT / "app" / "ml" / "models"
METRICS_PATH = MODEL_DIR / "intent_metrics.json"

LABELS = ["DIAGNOSIS", "QUOTATION", "APPOINTMENT", "WARRANTY", "TRACKING", "UNKNOWN"]


def load_dataset(path: Path) -> tuple[list[str], list[str], Counter[str]]:
    if not path.exists():
        raise FileNotFoundError(f"Dataset not found: {path}")
    texts: list[str] = []
    labels: list[str] = []
    sources: Counter[str] = Counter()
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        if reader.fieldnames is None or "text" not in reader.fieldnames or "label" not in reader.fieldnames:
            raise ValueError(f"Unexpected CSV header in {path}: {reader.fieldnames}")
        for idx, row in enumerate(reader, start=2):
            raw_text = (row.get("text") or "").strip()
            label = (row.get("label") or "").strip()
            source = (row.get("source") or "unknown").strip()
            if not raw_text:
                raise ValueError(f"Empty text at line {idx}")
            if label not in LABELS:
                raise ValueError(f"Invalid label {label!r} at line {idx}")
            texts.append(normalize_text(raw_text))
            labels.append(label)
            sources[source] += 1
    return texts, labels, sources


def _build_pipeline() -> Pipeline:
    return Pipeline([
        (
            "tfidf",
            TfidfVectorizer(
                analyzer="char_wb",
                ngram_range=(2, 5),
                min_df=1,
                max_features=20000,
                sublinear_tf=True,
            ),
        ),
        (
            "clf",
            LogisticRegression(
                max_iter=2000,
                class_weight="balanced",
                C=8.0,
                random_state=42,
            ),
        ),
    ])


def _benchmark_predictor(
    name: str,
    predict_fn,
    texts: list[str],
    y_true: list[str],
) -> dict:
    latencies_ms: list[float] = []
    y_pred: list[str] = []
    for text in texts:
        t0 = time.perf_counter()
        pred = predict_fn(text)
        latencies_ms.append((time.perf_counter() - t0) * 1000.0)
        y_pred.append(pred)

    report = classification_report(
        y_true,
        y_pred,
        labels=LABELS,
        output_dict=True,
        zero_division=0,
    )
    per_class = {
        lbl: {
            "precision": round(float(report[lbl]["precision"]), 4),
            "recall": round(float(report[lbl]["recall"]), 4),
            "f1": round(float(report[lbl]["f1-score"]), 4),
            "support": int(report[lbl]["support"]),
        }
        for lbl in LABELS
    }
    cm = confusion_matrix(y_true, y_pred, labels=LABELS).tolist()
    arr = np.array(latencies_ms, dtype=float)
    return {
        "model": name,
        "sampleCount": len(texts),
        "accuracy": round(float(accuracy_score(y_true, y_pred)), 4),
        "macroF1": round(float(f1_score(y_true, y_pred, labels=LABELS, average="macro", zero_division=0)), 4),
        "weightedF1": round(float(f1_score(y_true, y_pred, labels=LABELS, average="weighted", zero_division=0)), 4),
        "perClass": per_class,
        "confusionMatrix": {"labels": LABELS, "matrix": cm},
        "latencyMs": {
            "mean": round(float(arr.mean()), 4),
            "p95": round(float(np.percentile(arr, 95)), 4),
            "max": round(float(arr.max()), 4),
        },
        "mismatches": [
            {"text": txt, "expected": exp, "predicted": pred}
            for txt, exp, pred in zip(texts, y_true, y_pred)
            if exp != pred
        ],
    }


def _rule_predict(text: str) -> str:
    return classify_by_rules(text).intent.value


def _ml_predict_factory(ml_clf: MlIntentClassifier):
    def _predict(text: str) -> str:
        intent, _, _ = ml_clf.predict(text)
        return intent.value

    return _predict


def train_and_evaluate() -> dict:
    texts, labels, source_counts = load_dataset(DATASET_PATH)

    X_train, X_test, y_train, y_test = train_test_split(
        texts,
        labels,
        test_size=0.20,
        random_state=42,
        stratify=labels,
    )

    cv = StratifiedKFold(n_splits=5, shuffle=True, random_state=42)
    cv_scores = cross_val_score(_build_pipeline(), X_train, y_train, cv=cv, scoring="f1_macro")

    # Fit on training split first to benchmark fairly without peeking at X_test.
    split_pipeline = _build_pipeline()
    split_pipeline.fit(X_train, y_train)
    MODEL_DIR.mkdir(parents=True, exist_ok=True)
    split_tmp_path = MODEL_DIR / "_split_tmp_clf.joblib"
    joblib.dump(split_pipeline, split_tmp_path)

    split_ml = MlIntentClassifier(model_path=split_tmp_path)
    split_ml_predict = _ml_predict_factory(split_ml)

    # Temporarily swap singleton model path inside app.ml.intent_classifier for clean Hybrid split eval
    import app.ml.intent_classifier as ic_mod

    old_clf = ic_mod._ML_CLASSIFIER
    ic_mod._ML_CLASSIFIER = split_ml
    try:
        bench_test_split = {
            "ModelA_RuleBased": _benchmark_predictor("ModelA_RuleBased", _rule_predict, X_test, y_test),
            "ModelB_MLClassifier": _benchmark_predictor("ModelB_MLClassifier", split_ml_predict, X_test, y_test),
            "ModelC_Hybrid": _benchmark_predictor(
                "ModelC_Hybrid", lambda t: classify_intent(t).intent.value, X_test, y_test
            ),
        }
    finally:
        ic_mod._ML_CLASSIFIER = old_clf
        if split_tmp_path.exists():
            split_tmp_path.unlink()

    # Refit final production pipeline on full 660-sample training dataset and save.
    final_pipeline = _build_pipeline()
    final_pipeline.fit(texts, labels)
    joblib.dump(final_pipeline, MODEL_PATH)

    # Reset singleton so runtime uses the newly saved MODEL_PATH
    ic_mod._ML_CLASSIFIER = MlIntentClassifier(model_path=MODEL_PATH)
    prod_ml_predict = _ml_predict_factory(ic_mod._ML_CLASSIFIER)

    # Benchmark on independent eval_holdout.json (90 unseen cases)
    holdout_raw = json.loads(EVAL_HOLDOUT_PATH.read_text(encoding="utf-8"))
    holdout_data = holdout_raw["cases"] if isinstance(holdout_raw, dict) else holdout_raw
    h_texts = [item["message"] for item in holdout_data]
    h_labels = [item["expectedIntent"] for item in holdout_data]
    bench_holdout = {
        "ModelA_RuleBased": _benchmark_predictor("ModelA_RuleBased", _rule_predict, h_texts, h_labels),
        "ModelB_MLClassifier": _benchmark_predictor("ModelB_MLClassifier", prod_ml_predict, h_texts, h_labels),
        "ModelC_Hybrid": _benchmark_predictor(
            "ModelC_Hybrid", lambda t: classify_intent(t).intent.value, h_texts, h_labels
        ),
    }

    # Benchmark on eval_cases.json (30 acceptance cases)
    eval_raw = json.loads(EVAL_CASES_PATH.read_text(encoding="utf-8"))
    eval_cases = eval_raw["cases"] if isinstance(eval_raw, dict) else eval_raw
    e_texts = [item["message"] for item in eval_cases]
    e_labels = [item["expectedIntent"] for item in eval_cases]
    bench_eval_cases = {
        "ModelA_RuleBased": _benchmark_predictor("ModelA_RuleBased", _rule_predict, e_texts, e_labels),
        "ModelB_MLClassifier": _benchmark_predictor("ModelB_MLClassifier", prod_ml_predict, e_texts, e_labels),
        "ModelC_Hybrid": _benchmark_predictor(
            "ModelC_Hybrid", lambda t: classify_intent(t).intent.value, e_texts, e_labels
        ),
    }

    metrics = {
        "modelType": "TfidfVectorizer(char_wb, 2..5) + LogisticRegression(balanced, C=8.0)",
        "datasetSize": len(texts),
        "trainSize": len(X_train),
        "testSplitSize": len(X_test),
        "labelCounts": dict(Counter(labels)),
        "sourceCounts": dict(source_counts),
        "labels": LABELS,
        "cv5MacroF1Mean": round(float(cv_scores.mean()), 4),
        "cv5MacroF1Std": round(float(cv_scores.std()), 4),
        "testSplitBenchmark": bench_test_split,
        "evalHoldoutBenchmark": bench_holdout,
        "evalCasesBenchmark": bench_eval_cases,
    }
    METRICS_PATH.write_text(json.dumps(metrics, indent=2, ensure_ascii=False), encoding="utf-8")
    return metrics


if __name__ == "__main__":
    result = train_and_evaluate()
    summary = {
        "datasetSize": result["datasetSize"],
        "cv5MacroF1Mean": result["cv5MacroF1Mean"],
        "testSplit": {
            k: {"acc": v["accuracy"], "macroF1": v["macroF1"], "meanMs": v["latencyMs"]["mean"]}
            for k, v in result["testSplitBenchmark"].items()
        },
        "evalHoldout": {
            k: {"acc": v["accuracy"], "macroF1": v["macroF1"], "meanMs": v["latencyMs"]["mean"]}
            for k, v in result["evalHoldoutBenchmark"].items()
        },
        "evalCases": {
            k: {"acc": v["accuracy"], "macroF1": v["macroF1"], "meanMs": v["latencyMs"]["mean"]}
            for k, v in result["evalCasesBenchmark"].items()
        },
    }
    print(json.dumps(summary, indent=2, ensure_ascii=False))
