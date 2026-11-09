"""
Huấn luyện bộ phân loại intent cho Coordinator (Task 2.1/2.3).

Chạy (từ thư mục ai-service):
    .\\venv\\Scripts\\python.exe scripts\\train_intent_classifier.py

Kết quả:
- app/ml/models/intent_clf.joblib
- app/ml/models/intent_metrics.json
- In bảng metric (accuracy, macro-F1) trên tập holdout 20% + so sánh với rule-based.

Lưu ý trung thực số liệu: metric chỉ tính trên holdout stratified 20% của
`data/intent_dataset.csv`; bộ `data/eval_cases.json` là holdout độc lập thứ hai
(nhãn tay) dùng trong test_intent_quality.
"""

from __future__ import annotations

import csv
import json
import sys
from collections import Counter
from pathlib import Path

AI_SERVICE_DIR = Path(__file__).resolve().parents[1]
if str(AI_SERVICE_DIR) not in sys.path:
    sys.path.insert(0, str(AI_SERVICE_DIR))

DATA_PATH = AI_SERVICE_DIR / "data" / "intent_dataset.csv"
MODEL_DIR = AI_SERVICE_DIR / "app" / "ml" / "models"
MODEL_PATH = MODEL_DIR / "intent_clf.joblib"
METRICS_PATH = MODEL_DIR / "intent_metrics.json"

LABELS = ["DIAGNOSIS", "QUOTATION", "APPOINTMENT", "TRACKING", "UNKNOWN"]


def load_dataset() -> tuple[list[str], list[str]]:
    texts: list[str] = []
    labels: list[str] = []
    with DATA_PATH.open(encoding="utf-8", newline="") as handle:
        reader = csv.DictReader(handle)
        if reader.fieldnames != ["text", "label"]:
            raise SystemExit(f"Header CSV không đúng: {reader.fieldnames}")
        for row in reader:
            text = (row["text"] or "").strip()
            label = (row["label"] or "").strip()
            if not text or label not in LABELS:
                raise SystemExit(f"Dòng không hợp lệ: {row}")
            texts.append(text)
            labels.append(label)
    return texts, labels


def main() -> int:
    from sklearn.feature_extraction.text import TfidfVectorizer
    from sklearn.linear_model import LogisticRegression
    from sklearn.metrics import accuracy_score, classification_report, confusion_matrix
    from sklearn.model_selection import StratifiedKFold, cross_val_score, train_test_split
    from sklearn.pipeline import Pipeline

    import joblib

    texts, labels = load_dataset()
    distribution = Counter(labels)
    print(f"Dataset: {len(texts)} câu | phân bố: {dict(sorted(distribution.items()))}")

    def build_pipeline() -> Pipeline:
        return Pipeline(
            [
                (
                    "tfidf",
                    TfidfVectorizer(
                        analyzer="char_wb", ngram_range=(2, 4), sublinear_tf=True
                    ),
                ),
                (
                    "clf",
                    LogisticRegression(
                        max_iter=2000, class_weight="balanced", C=10.0, random_state=42
                    ),
                ),
            ]
        )

    x_train, x_test, y_train, y_test = train_test_split(
        texts, labels, test_size=0.2, random_state=42, stratify=labels
    )

    pipeline = build_pipeline()
    pipeline.fit(x_train, y_train)
    predictions = pipeline.predict(x_test)

    accuracy = accuracy_score(y_test, predictions)
    report = classification_report(
        y_test, predictions, labels=LABELS, output_dict=True, zero_division=0
    )
    matrix = confusion_matrix(y_test, predictions, labels=LABELS).tolist()

    # So sánh công bằng: rule-only trên CÙNG tập holdout.
    from app.ml.intent_classifier import classify_by_rules

    rule_predictions = [classify_by_rules(text).intent.value for text in x_test]
    rule_accuracy = accuracy_score(y_test, rule_predictions)

    cv = StratifiedKFold(n_splits=5, shuffle=True, random_state=42)
    cv_scores = cross_val_score(build_pipeline(), texts, labels, cv=cv, scoring="accuracy")

    final_pipeline = build_pipeline()
    final_pipeline.fit(texts, labels)
    MODEL_DIR.mkdir(parents=True, exist_ok=True)
    joblib.dump(final_pipeline, MODEL_PATH)

    metrics = {
        "generatedAt": "2026-11-09",
        "datasetSize": len(texts),
        "labelDistribution": dict(sorted(distribution.items())),
        "holdout": {
            "size": len(x_test),
            "accuracy": round(float(accuracy), 4),
            "macroF1": round(float(report["macro avg"]["f1-score"]), 4),
            "weightedF1": round(float(report["weighted avg"]["f1-score"]), 4),
            "perClass": {
                label: {
                    "precision": round(float(report[label]["precision"]), 4),
                    "recall": round(float(report[label]["recall"]), 4),
                    "f1": round(float(report[label]["f1-score"]), 4),
                    "support": int(report[label]["support"]),
                }
                for label in LABELS
            },
            "confusionMatrix": matrix,
        },
        "ruleBaselineOnSameHoldout": {"accuracy": round(float(rule_accuracy), 4)},
        "crossValidation5Fold": {
            "folds": [round(float(score), 4) for score in cv_scores],
            "mean": round(float(cv_scores.mean()), 4),
            "std": round(float(cv_scores.std()), 4),
        },
        "model": "TfidfVectorizer(char_wb 2-4) + LogisticRegression(C=10, balanced)",
    }
    METRICS_PATH.write_text(
        json.dumps(metrics, ensure_ascii=False, indent=2), encoding="utf-8"
    )

    print(f"Holdout accuracy : {accuracy:.4f} (n={len(x_test)})")
    print(f"Holdout macro-F1 : {report['macro avg']['f1-score']:.4f}")
    print(f"Rule-only acc    : {rule_accuracy:.4f} (cùng holdout)")
    print(f"CV 5-fold        : {cv_scores.mean():.4f} ± {cv_scores.std():.4f}")
    print(f"Saved model      : {MODEL_PATH.relative_to(AI_SERVICE_DIR)}")
    print(f"Saved metrics    : {METRICS_PATH.relative_to(AI_SERVICE_DIR)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
