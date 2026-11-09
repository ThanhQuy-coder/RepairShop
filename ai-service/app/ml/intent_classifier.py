"""
Bộ phân loại intent cho Coordinator Agent (Task 2.1).

Thiết kế lai (hybrid) theo HUONG_DAN_TRIEN_KHAI_MAS_4_NGAY.md mục 4.3.4:
- **Rule** xử lý các câu có tín hiệu mạnh (≥3 điểm) => nhanh, deterministic, không phụ thuộc model.
- **scikit-learn** (TF-IDF char n-gram + LogisticRegression) xử lý câu mơ hồ (1–2 điểm).
- Nếu model chưa tồn tại / thiếu scikit-learn => **fallback rule**, không crash.

Nguyên tắc: classifier KHÔNG được gọi LLM (giữ ngân sách 1 LLM call/request cho Diagnosis).
"""

from __future__ import annotations

import logging
from dataclasses import dataclass, field
from pathlib import Path
from typing import Dict, List, Optional, Tuple

from app.core.text import contains_term, normalize_text
from app.schemas.agent_models import Intent

logger = logging.getLogger(__name__)

MODEL_PATH = Path(__file__).resolve().parent / "models" / "intent_clf.joblib"

# Ngưỡng quyết định
MIN_RULE_CONFIDENCE = 0.85   # rule mạnh => dùng luôn, không cần ML
MIN_ML_CONFIDENCE = 0.55     # ML phải đủ tự tin mới được ghi đè rule yếu

# ── Từ điển tín hiệu (đã cân nhắc các câu khó: "error code", "thanh toán", "QR code") ──
_DIAGNOSIS_TERMS: Dict[str, int] = {
    # triệu chứng
    "pin": 1, "chai pin": 4, "pin chai": 4, "chai": 1, "tụt pin": 2, "hao pin": 2, "tụt": 1,
    "sạc": 1, "không sạc": 2, "sạc không vào": 2, "chân sạc": 2, "cổng sạc": 2,
    "màn hình": 2, "cảm ứng": 2, "vỡ màn": 2, "nứt màn": 2, "sọc màn": 2,
    "bàn phím": 2, "phím": 1, "loa": 1, "âm thanh": 1, "mic": 1, "micro": 1,
    "camera": 1, "mờ": 1, "nóng": 1, "quá nhiệt": 2, "nóng máy": 2,
    "tự tắt": 2, "tắt nguồn": 2, "không lên": 2, "không mở được": 2,
    "treo": 1, "giật": 1, "lag": 1, "chậm": 1, "đơ": 1,
    "nước": 1, "vô nước": 2, "rơi": 1, "hỏng": 1, "lỗi": 1, "mã lỗi": 2,
    "error code": 2, "hiện tượng": 1, "chập chờn": 1, "không nhận": 2,
    "vân tay": 1, "sóng": 1, "wifi": 1, "bluetooth": 1, "rung": 1, "chuông": 1,
    "bụi": 1, "quạt": 1, "bản lề": 1, "bản lề": 1, "phồng": 1, "liệt": 1,
    # thiết bị / hành động chẩn đoán
    "máy": 1, "điện thoại": 1, "laptop": 1, "ipad": 1, "tablet": 1,
    "chẩn đoán": 2, "thay pin": 2,
    "thay màn": 2, "thay màn hình": 2, "bị lỗi": 2, "bị hư": 1, "hư": 1,
}

_QUOTATION_TERMS: Dict[str, int] = {
    "giá": 1, "báo giá": 2, "giá cả": 2, "bao nhiêu": 2, "chi phí": 2,
    "phí": 1, "tiền": 1, "giá tham khảo": 2, "hết bao nhiêu": 2,
    "tốn bao nhiêu": 2, "rẻ": 1, "đắt": 1, "bảng giá": 2, "dự toán": 2,
    "ước tính": 2, "tổng tiền": 2, "tính tiền": 2, "giá sao": 1,
    "chi phí thay": 2, "giá thay": 2, "bao nhiêu tiền": 2,
    "biết giá": 3, "chênh nhau": 2, "giá bao nhiêu": 4,
}

_APPOINTMENT_TERMS: Dict[str, int] = {
    "đặt lịch": 3, "đặt hẹn": 3, "đặt lịch hẹn": 3, "lịch hẹn": 2,
    "khung giờ": 2, "còn chỗ": 2, "còn slot": 2, "slot": 1,
    "ngày mai": 2, "ngày kia": 2, "chiều nay": 1, "sáng nay": 1,
    "mấy giờ": 2, "lúc mấy giờ": 2, "buổi sáng": 1, "buổi chiều": 1,
    "3h chiều": 2, "2h chiều": 2, "sáng mai": 2, "chiều mai": 2,
    "đặt": 1, "hẹn": 1, "book": 2, "còn trống": 2, "giờ nào": 1,
    "mang máy": 2, "tuần sau": 1,
    "thứ hai": 1, "thứ ba": 1, "thứ tư": 1, "thứ năm": 1, "thứ sáu": 1,
    "thứ bảy": 1, "chủ nhật": 1,
}

_TRACKING_TERMS: Dict[str, int] = {
    "tra cứu": 3, "mã tra cứu": 3, "mã đơn": 3, "mã phiếu": 3,
    "tiến độ": 3, "tình trạng sửa": 3, "đơn hàng": 2, "phiếu sửa": 2,
    "rs-": 3, "lookup": 2, "kiểm tra đơn": 2, "đến đâu rồi": 3,
    "sửa xong chưa": 3, "bao giờ xong": 3, "trạng thái": 1, "theo dõi": 2,
    "ticket": 1, "mã số": 2, "tới đâu": 3, "đến đâu": 3, "bước nào": 3,
    "sửa xong": 3, "đơn": 2, "đã xong chưa": 2,
}

_TERM_GROUPS: Tuple[Tuple[Intent, Dict[str, int]], ...] = (
    (Intent.DIAGNOSIS, _DIAGNOSIS_TERMS),
    (Intent.QUOTATION, _QUOTATION_TERMS),
    (Intent.APPOINTMENT, _APPOINTMENT_TERMS),
    (Intent.TRACKING, _TRACKING_TERMS),
)


def normalize_text(text: str) -> str:  # re-export cho nơi khác dùng
    from app.core.text import normalize_text as _normalize

    return _normalize(text)


def _contains(haystack: str, term: str) -> bool:
    return contains_term(haystack, term)


@dataclass
class IntentSignals:
    intent: Intent
    confidence: float
    scores: Dict[str, int] = field(default_factory=dict)
    matched_terms: Dict[str, List[str]] = field(default_factory=dict)
    secondary: List[Intent] = field(default_factory=list)
    source: str = "rule"


def _score_terms(text: str) -> Tuple[Dict[str, int], Dict[str, List[str]]]:
    """
    Chấm điểm theo từ khóa, ưu tiên cụm dài trước và BỎ QUA từ khóa con đã được cụm dài bao phủ.

    Lý do: nếu cộng cả "thay pin" (2) lẫn "pin" (1), câu "giá thay pin bao nhiêu" sẽ bị
    đẩy về DIAGNOSIS oan uổng dù người dùng hỏi giá.
    """
    scores: Dict[str, int] = {}
    matched: Dict[str, List[str]] = {}

    for intent, terms in _TERM_GROUPS:
        score = 0
        hits: List[str] = []
        for term in sorted(terms, key=len, reverse=True):
            if any(term in longer for longer in hits):
                continue
            if contains_term(text, term):
                score += terms[term]
                hits.append(term)
        scores[intent.value] = score
        matched[intent.value] = hits
    return scores, matched


def _confidence_from_score(score: int, margin: int) -> float:
    if score <= 0:
        return 0.2
    if margin <= 0:
        return 0.45
    base = {1: 0.55, 2: 0.7}.get(score, 0.85 + min(0.1, (score - 3) * 0.05))
    return round(min(0.95, base), 2)


def _secondary(scores: Dict[str, int], primary: Intent) -> List[Intent]:
    """Các intent có tín hiệu đủ mạnh (>0) nhưng không phải primary — dùng cho routing mở rộng."""
    result: List[Intent] = []
    for intent in (Intent.DIAGNOSIS, Intent.QUOTATION, Intent.APPOINTMENT):
        if intent is primary:
            continue
        if scores.get(intent.value, 0) > 0:
            result.append(intent)
    return result


def classify_by_rules(message: str) -> IntentSignals:
    """Phân loại bằng luật từ khóa, có xử lý ưu tiên cho câu vừa tả lỗi vừa hỏi giá/đặt lịch."""
    text = normalize_text(message)
    scores, matched = _score_terms(text)

    diagnosis = scores[Intent.DIAGNOSIS.value]
    quotation = scores[Intent.QUOTATION.value]
    appointment = scores[Intent.APPOINTMENT.value]
    tracking = scores[Intent.TRACKING.value]

    intent = Intent.UNKNOWN
    if tracking >= 3 and (quotation + appointment + diagnosis) < tracking:
        intent = Intent.TRACKING
    elif quotation >= 3 and quotation >= diagnosis:
        # Hỏi giá rõ ràng (>= 2 tín hiệu giá) => ưu tiên QUOTATION, kể cả khi có tên dịch vụ.
        intent = Intent.QUOTATION
    elif diagnosis >= 3:
        # Tả lỗi rõ ràng => ưu tiên chẩn đoán, kể cả khi có hỏi thêm giá/giờ
        intent = Intent.DIAGNOSIS
    elif appointment >= 2 and appointment > diagnosis and appointment > quotation:
        intent = Intent.APPOINTMENT
    elif quotation >= 2 and quotation >= diagnosis:
        intent = Intent.QUOTATION
    elif diagnosis >= 1:
        intent = Intent.DIAGNOSIS
    elif appointment >= 1:
        intent = Intent.APPOINTMENT
    elif quotation >= 1:
        intent = Intent.QUOTATION
    elif tracking >= 1:
        intent = Intent.TRACKING

    ordered = sorted(scores.values(), reverse=True)
    margin = ordered[0] - ordered[1] if len(ordered) > 1 else ordered[0]
    primary_score = scores[intent.value] if intent is not Intent.UNKNOWN else 0

    return IntentSignals(
        intent=intent,
        confidence=_confidence_from_score(primary_score, margin),
        scores=scores,
        matched_terms=matched,
        secondary=_secondary(scores, intent),
        source="rule",
    )


class MlIntentClassifier:
    """TF-IDF + LogisticRegression; import sklearn LAZY để không làm chậm cold start."""

    def __init__(self, model_path: Path = MODEL_PATH) -> None:
        self._model_path = model_path
        self._model = None
        self._load_failed = False

    @property
    def is_available(self) -> bool:
        return self._model_path.is_file() and not self._load_failed

    def _ensure_loaded(self) -> bool:
        if self._model is not None:
            return True
        if self._load_failed or not self._model_path.is_file():
            return False
        try:
            import joblib  # type: ignore

            self._model = joblib.load(self._model_path)
            return True
        except Exception as exc:  # pragma: no cover - phụ thuộc môi trường
            logger.warning("Không load được intent model (%s): %s", self._model_path, exc)
            self._load_failed = True
            return False

    def predict(self, message: str) -> Optional[Tuple[Intent, float]]:
        if not self._ensure_loaded():
            return None
        try:
            probabilities = self._model.predict_proba([message])[0]
            classes = list(self._model.classes_)
            best_index = int(probabilities.argmax())
            return Intent(str(classes[best_index])), float(probabilities[best_index])
        except Exception as exc:  # pragma: no cover
            logger.warning("Lỗi khi dự đoán intent bằng model: %s", exc)
            return None


_ml_classifier = MlIntentClassifier()


def get_ml_classifier() -> MlIntentClassifier:
    return _ml_classifier


def classify_intent(message: str) -> IntentSignals:
    """
    Kết hợp rule + ML:
    - Rule mạnh (≥3 điểm, confidence ≥ 0.85) => dùng rule (nhanh, ổn định).
    - Ngược lại, nếu model đủ tự tin (≥0.55) => dùng model.
    - Còn lại => rule (kể cả UNKNOWN) để không bao giờ "đoán bừa".
    """
    rule = classify_by_rules(message)

    # Tín hiệu chẩn đoán mạnh (>=3) phải thắng cả khi điểm hoà với đặt lịch/báo giá:
    # người dùng vừa tả lỗi vừa hỏi giá/đặt lịch thì vẫn cần chẩn đoán trước (plan sẽ thêm Q/A).
    if rule.intent is Intent.DIAGNOSIS and rule.scores.get(Intent.DIAGNOSIS.value, 0) >= 3:
        return rule

    if rule.intent is not Intent.UNKNOWN and rule.confidence >= MIN_RULE_CONFIDENCE:
        return rule

    prediction = _ml_classifier.predict(message)
    if prediction is not None:
        intent, confidence = prediction
        if confidence >= MIN_ML_CONFIDENCE:
            return IntentSignals(
                intent=intent,
                confidence=round(confidence, 2),
                scores=rule.scores,
                matched_terms=rule.matched_terms,
                secondary=_secondary(rule.scores, intent),
                source="ml",
            )

    return rule
