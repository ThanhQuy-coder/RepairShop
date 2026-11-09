"""Luật suy luận nguyên nhân khả dĩ cho Diagnosis Agent (fallback offline + gợi ý).

Mục đích: khi không có LLM (hoặc LLM lỗi), agent vẫn trả về kết quả có căn cứ từ
danh mục triệu chứng — KHÔNG bịa ID, KHÔNG kết luận chắc chắn.
"""

from __future__ import annotations

from typing import List, Tuple

from app.core.text import contains_term, normalize_text
from app.schemas.agent_models import Confidence, PossibleCause

# (các từ khóa, tên nguyên nhân khả dĩ, độ tin cậy)
CAUSE_RULES: Tuple[Tuple[Tuple[str, ...], str, Confidence], ...] = (
    (
        ("pin", "chai", "tụt", "hao", "phồng pin", "pin yếu"),
        "Pin chai hoặc suy giảm dung lượng",
        Confidence.HIGH,
    ),
    (
        ("sạc", "chân sạc", "cổng sạc", "không sạc", "sạc không vào"),
        "Chân sạc lỏng/hỏng hoặc cáp sạc không ổn định",
        Confidence.MEDIUM,
    ),
    (
        ("màn hình", "cảm ứng", "vỡ", "nứt", "sọc", "đốm", "mực"),
        "Hỏng màn hình hoặc lớp cảm ứng",
        Confidence.MEDIUM,
    ),
    (
        ("bàn phím", "phím", "gõ"),
        "Hỏng bàn phím hoặc kết nối bàn phím",
        Confidence.MEDIUM,
    ),
    (
        ("nóng", "quạt", "tản nhiệt", "bụi", "keo tản nhiệt"),
        "Bụi bẩn hoặc keo tản nhiệt khô làm máy nóng",
        Confidence.MEDIUM,
    ),
    (
        ("loa", "âm thanh", "mic", "micro", "rè"),
        "Hỏng loa hoặc micro",
        Confidence.MEDIUM,
    ),
    (
        ("camera", "mờ", "đen", "không lấy nét"),
        "Hỏng camera hoặc module camera",
        Confidence.MEDIUM,
    ),
    (
        ("nước", "vô nước", "rơi nước", "ẩm"),
        "Thiết bị từng tiếp xúc nước/ẩm",
        Confidence.MEDIUM,
    ),
    (
        ("treo", "chậm", "giật", "lag", "đơ", "khởi động lại"),
        "Lỗi phần mềm hoặc bộ nhớ trong",
        Confidence.LOW,
    ),
    (
        ("nguồn", "không lên", "tắt nguồn", "tự tắt", "sập nguồn"),
        "Sự cố nguồn hoặc mạch cấp nguồn",
        Confidence.MEDIUM,
    ),
)

MANUAL_CHECK_CAUSE = PossibleCause(
    name="Cần kiểm tra trực tiếp để xác định nguyên nhân",
    confidence=Confidence.LOW,
)


def infer_causes(issue_description: str, limit: int = 3) -> List[PossibleCause]:
    """Suy ra tối đa `limit` nguyên nhân khả dĩ, xếp theo số từ khóa khớp."""
    text = normalize_text(issue_description)
    scored: List[Tuple[int, PossibleCause]] = []

    for terms, cause_name, confidence in CAUSE_RULES:
        hits = sum(1 for term in terms if contains_term(text, term))
        if hits:
            scored.append((hits, PossibleCause(name=cause_name, confidence=confidence)))

    if not scored:
        return [MANUAL_CHECK_CAUSE.model_copy()]

    scored.sort(key=lambda item: item[0], reverse=True)
    causes: List[PossibleCause] = []
    seen = set()
    for _, cause in scored:
        if cause.name in seen:
            continue
        seen.add(cause.name)
        causes.append(cause)
        if len(causes) >= limit:
            break
    return causes


def build_explanation(issue_description: str, causes: List[PossibleCause]) -> str:
    """Câu giải thích ≤300 ký tự, chỉ mang tính tham khảo (không khẳng định)."""
    if not issue_description.strip():
        return "Chưa có mô tả tình trạng, cần thêm thông tin để tư vấn."
    if causes and causes[0].name != MANUAL_CHECK_CAUSE.name:
        return (
            f"Mô tả phù hợp với khả năng: {causes[0].name.lower()}. "
            "Đây là đánh giá tham khảo, kỹ thuật viên cần kiểm tra trực tiếp."
        )[:300]
    return (
        "Mô tả chưa đủ dữ liệu để xác định nguyên nhân. "
        "Nên kiểm tra trực tiếp tại cửa hàng để có kết luận chính xác."
    )[:300]
