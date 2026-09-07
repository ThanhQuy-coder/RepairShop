"""
Bảng ánh xạ triệu chứng thường gặp -> từ khóa dịch vụ/linh kiện liên quan.
Đây KHÔNG phải embedding/semantic search thật — chỉ là 1 lớp mở rộng từ khóa đơn giản,
đủ để giải quyết case "pin tụt nhanh" phải liên kết được với dịch vụ tên "Thay pin"
dù 2 chuỗi này không overlap từ nào theo nghĩa đen (tụt nhanh != thay).
"""

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


def expand_issue_keywords(issue_tokens: set[str]) -> set[str]:
    """Mở rộng tập từ khóa của issueDescription bằng bảng triệu chứng — dùng để chấm điểm candidate."""
    expanded = set(issue_tokens)
    for symptom, related_terms in SYMPTOM_KEYWORD_MAP.items():
        if any(symptom in token or token in symptom for token in issue_tokens):
            expanded.update(related_terms)
    return expanded