"""
Thực nghiệm scikit-learn cho Diagnosis Agent (Task 2.2).

Bài toán: từ mô tả triệu chứng (tiếng Việt) -> nhóm nguyên nhân/linh kiện
(pin, man_hinh, ban_phim, sac, tan_nhiet, loa, camera, khac).

Chạy (từ thư mục ai-service):
    .\\venv\\Scripts\\python.exe scripts\\train_diagnosis_classifier.py

Kết quả:
- app/ml/models/diagnosis_clf.joblib
- app/ml/models/diagnosis_metrics.json (5-fold CV + per-class + confusion matrix)

Giới hạn trung thực: dataset 96 dòng do nhóm tự gán nhãn, KHÔNG phải dữ liệu thu thập
thực tế từ cửa hàng. Vì vậy metric chỉ có ý nghĩa "chứng minh pipeline học được",
không dùng để tuyên bố độ chính xác ngoài thực tế.
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

DATA_PATH = AI_SERVICE_DIR / "data" / "diagnosis_labels.csv"
MODEL_DIR = AI_SERVICE_DIR / "app" / "ml" / "models"
MODEL_PATH = MODEL_DIR / "diagnosis_clf.joblib"
METRICS_PATH = MODEL_DIR / "diagnosis_metrics.json"


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
            if not text or not label:
                raise SystemExit(f"Dòng không hợp lệ: {row}")
            texts.append(text)
            labels.append(label)
    return texts, labels


def main() -> int:
    from sklearn.feature_extraction.text import TfidfVectorizer
    from sklearn.linear_model import LogisticRegression
    from sklearn.metrics import classification_report, confusion_matrix
    from sklearn.model_selection import StratifiedKFold, cross_val_predict, cross_val_score
    from sklearn.pipeline import Pipeline

    import joblib

    texts, labels = load_dataset()
    distribution = Counter(labels)
    print(f"Dataset: {len(texts)} dòng | {len(distribution)} nhãn | {dict(sorted(distribution.items()))}")

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

    labels_order = sorted(distribution)
    cv = StratifiedKFold(n_splits=5, shuffle=True, random_state=42)
    scores = cross_val_score(build_pipeline(), texts, labels, cv=cv, scoring="accuracy")
    predictions = cross_val_predict(build_pipeline(), texts, labels, cv=cv)

    report = classification_report(
        labels, predictions, labels=labels_order, output_dict=True, zero_division=0
    )
    matrix = confusion_matrix(labels, predictions, labels=labels_order).tolist()

    final_pipeline = build_pipeline()
    final_pipeline.fit(texts, labels)
    MODEL_DIR.mkdir(parents=True, exist_ok=True)
    joblib.dump(final_pipeline, MODEL_PATH)

    metrics = {
        "generatedAt": "2026-11-09",
        "datasetSize": len(texts),
        "labelDistribution": dict(sorted(distribution.items())),
        "crossValidation5Fold": {
            "folds": [round(float(score), 4) for score in scores],
            "meanAccuracy": round(float(scores.mean()), 4),
            "std": round(float(scores.std()), 4),
            "macroF1": round(float(report["macro avg"]["f1-score"]), 4),
            "perClass": {
                label: {
                    "precision": round(float(report[label]["precision"]), 4),
                    "recall": round(float(report[label]["recall"]), 4),
                    "f1": round(float(report[label]["f1-score"]), 4),
                    "support": int(report[label]["support"]),
                }
                for label in labels_order
            },
            "confusionMatrix": matrix,
            "labelsOrder": labels_order,
        },
        "model": "TfidfVectorizer(char_wb 2-4) + LogisticRegression(C=10, balanced)",
        "caveat": "Nhãn do nhóm tự gán (96 dòng), không phải dữ liệu thu thập thực tế.",
    }
    METRICS_PATH.write_text(
        json.dumps(metrics, ensure_ascii=False, indent=2), encoding="utf-8"
    )

    print(f"CV 5-fold accuracy: {scores.mean():.4f} ± {scores.std():.4f}")
    print(f"Macro-F1 (CV)     : {report['macro avg']['f1-score']:.4f}")
    print(f"Saved model       : {MODEL_PATH.relative_to(AI_SERVICE_DIR)}")
    print(f"Saved metrics     : {METRICS_PATH.relative_to(AI_SERVICE_DIR)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
