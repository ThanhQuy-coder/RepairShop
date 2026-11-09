"""
Bảng ánh xạ triệu chứng thường gặp -> từ khóa dịch vụ/linh kiện liên quan.
Đây KHÔNG phải embedding/semantic search thật — chỉ là 1 lớp mở rộng từ khóa đơn giản,
đủ để giải quyết case "pin tụt nhanh" phải liên kết được với dịch vụ tên "Thay pin"
dù 2 chuỗi này không overlap từ nào theo nghĩa đen (tụt nhanh != thay).
"""

from app.core.text import contains_term, normalize_text

SYMPTOM_KEYWORD_MAP: dict[str, list[str]] = {
    "tụt": ["pin", "sạc"],
    "chai": ["pin"],
    "nóng": ["pin", "main", "nguồn", "vệ sinh"],
    "sạc": ["pin", "cáp", "sạc", "chân sạc"],
    "tắt": ["pin", "nguồn", "main"],
    "nguồn": ["main", "nguồn", "pin"],
    "vỡ": ["màn hình", "kính", "mặt kính"],
    "nứt": ["màn hình", "kính"],
    "sọc": ["màn hình"],
    "cảm ứng": ["màn hình"],
    "camera": ["camera"],
    "mờ": ["camera"],
    "loa": ["loa"],
    "âm thanh": ["loa", "mic"],
    "mic": ["mic"],
    "nước": ["main", "vệ sinh"],
    "vô nước": ["main", "vệ sinh"],
    "chậm": ["main", "vệ sinh", "phần mềm"],
    "treo": ["main", "phần mềm"],
}


def expand_text_keywords(text: str) -> set[str]:
    """
    Mở rộng từ khóa từ CẢ CÂU (Task 2.2 — sửa D-07).

    Vì sao cần: `_tokenize` tách từ đơn nên "cảm ứng" thành {"cảm","ứng"} và không bao giờ
    khớp key ghép trong SYMPTOM_KEYWORD_MAP. Hàm này so khớp CỤM trên văn bản gốc, sau đó
    thêm cả cụm lẫn các token con để việc chấm điểm trên tên dịch vụ vẫn hoạt động.
    """
    lowered = normalize_text(text)
    expanded: set[str] = set()
    for symptom, related_terms in SYMPTOM_KEYWORD_MAP.items():
        if contains_term(lowered, symptom):
            expanded.update(related_terms)
            for term in related_terms:
                expanded.update(term.split())
            expanded.update(symptom.split())
    return expanded


def expand_issue_keywords(issue_tokens: set[str]) -> set[str]:
    """Giữ API cũ: mở rộng từ tập token (ghép lại thành câu để bắt được cụm tiếng Việt)."""
    return expand_text_keywords(" ".join(sorted(issue_tokens)))
