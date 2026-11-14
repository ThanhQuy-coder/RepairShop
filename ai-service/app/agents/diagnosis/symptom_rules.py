"""Luật suy luận nguyên nhân khả dĩ cho Diagnosis Agent (5 DeviceType, tiếng Việt có dấu & không dấu).

Mục đích: khi không có LLM (offline) hoặc LLM lỗi/fallback, agent vẫn trả về nhận định khả dĩ
có căn cứ từ 74 triệu chứng & ngữ cảnh — KHÔNG bịa ID, KHÔNG kết luận chắc chắn.
"""

from __future__ import annotations

from typing import List, Tuple

from app.core.text import contains_term_flexible, normalize_colloquial_vietnamese
from app.schemas.agent_models import Confidence, PossibleCause

# (các từ khóa, tên nguyên nhân khả dĩ, độ tin cậy)
CAUSE_RULES: Tuple[Tuple[Tuple[str, ...], str, Confidence], ...] = (
    (
        ("pin", "chai", "tụt", "hao", "phồng pin", "pin yếu", "tut pin", "chai pin"),
        "Pin chai hoặc suy giảm dung lượng",
        Confidence.HIGH,
    ),
    (
        ("sạc", "chân sạc", "cổng sạc", "không sạc", "sạc không vào", "sac chap chon"),
        "Chân sạc lỏng/hỏng hoặc mạch sạc không ổn định",
        Confidence.MEDIUM,
    ),
    (
        ("màn hình", "cảm ứng", "vỡ", "nứt", "rạn", "sọc", "đốm", "mực", "nhấp nháy", "ép kính"),
        "Hỏng màn hình, mặt kính hoặc lớp cảm ứng",
        Confidence.MEDIUM,
    ),
    (
        ("bàn phím", "phím", "gõ", "touchpad", "liệt phím"),
        "Hỏng bàn phím, touchpad hoặc cáp kết nối phím",
        Confidence.MEDIUM,
    ),
    (
        ("bản lề", "vỏ máy", "nắp lưng"),
        "Hỏng khớp bản lề hoặc kết cấu vỏ thiết bị",
        Confidence.MEDIUM,
    ),
    (
        ("nóng", "quạt", "tản nhiệt", "bụi", "keo tản nhiệt", "quá nhiệt"),
        "Bụi bẩn hệ thống tản nhiệt hoặc keo tản nhiệt khô làm máy nóng",
        Confidence.MEDIUM,
    ),
    (
        ("loa", "âm thanh", "mic", "micro", "rè", "nhiễu âm", "mất tiếng"),
        "Hỏng loa, micro hoặc mạch xử lý âm thanh",
        Confidence.MEDIUM,
    ),
    (
        ("camera", "mờ", "không lấy nét", "rung camera"),
        "Hỏng cụm camera hoặc kính bảo vệ ống kính",
        Confidence.MEDIUM,
    ),
    (
        ("nước", "vô nước", "rơi nước", "vào nước", "chất lỏng", "ẩm"),
        "Thiết bị có dấu hiệu tiếp xúc chất lỏng, cần vệ sinh/sấy mạch",
        Confidence.MEDIUM,
    ),
    (
        ("wifi", "bluetooth", "sóng", "sim", "esim", "gps", "không đồng bộ"),
        "Lỗi ăng-ten, mô-đun kết nối không dây hoặc khay SIM",
        Confidence.MEDIUM,
    ),
    (
        ("ssd", "ổ cứng", "ram", "hệ điều hành", "win", "firmware"),
        "Lỗi bộ nhớ, ổ lưu trữ hoặc xung đột hệ điều hành/firmware",
        Confidence.MEDIUM,
    ),
    (
        ("cảm biến", "nhịp tim", "núm xoay", "tay cầm", "trôi cần", "analog"),
        "Hao mòn cảm biến hoặc linh kiện điều khiển cơ học",
        Confidence.MEDIUM,
    ),
    (
        ("treo", "chậm", "giật", "lag", "đơ", "khởi động lại", "bootloop", "error code"),
        "Lỗi phần mềm/firmware hoặc quá tải bộ nhớ",
        Confidence.LOW,
    ),
    (
        ("nguồn", "không lên", "tắt nguồn", "tự tắt", "sập nguồn", "bo mạch", "main"),
        "Sự cố bo mạch hoặc mạch cấp nguồn",
        Confidence.MEDIUM,
    ),
)

MANUAL_CHECK_CAUSE = PossibleCause(
    name="Cần kiểm tra trực tiếp để xác định nguyên nhân",
    confidence=Confidence.LOW,
)


def infer_causes(issue_description: str, limit: int = 3) -> List[PossibleCause]:
    """Suy ra tối đa `limit` nguyên nhân khả dĩ từ văn bản có dấu / không dấu."""
    text = normalize_colloquial_vietnamese(issue_description)
    scored: List[Tuple[int, PossibleCause]] = []

    for terms, cause_name, confidence in CAUSE_RULES:
        hits = sum(1 for term in terms if contains_term_flexible(text, term))
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
    """Câu giải thích <=300 ký tự, chỉ mang tính tham khảo (không khẳng định chắc chắn)."""
    if not issue_description.strip():
        return "Chưa có mô tả tình trạng, cần thêm thông tin để tư vấn tham khảo."
    if causes and causes[0].name != MANUAL_CHECK_CAUSE.name:
        return (
            f"Mô tả phù hợp với khả năng: {causes[0].name.lower()}. "
            "Đây là nhận định tham khảo, kỹ thuật viên cần kiểm tra trực tiếp."
        )[:300]
    return (
        "Mô tả chưa đủ dữ liệu để khoanh vùng nguyên nhân. "
        "Nên kiểm tra trực tiếp tại cửa hàng để có đánh giá chính xác."
    )[:300]
