"""Tiện ích xử lý text dùng chung cho MAS (Task 2.1/2.2).

Tách riêng để intent classifier, diagnosis agent và retrieval dùng CÙNG một quy tắc
so khớp — tránh mỗi nơi một kiểu (nguồn gốc lỗi D-07: token đơn vs cụm từ).
"""

from __future__ import annotations

import re
import unicodedata
from typing import Set

_TOKEN_RE = re.compile(r"[0-9a-zA-ZÀ-ỹĐđ]+")
_BOUNDARY = "0-9a-zA-ZÀ-ỹĐđ"


def normalize_text(text: str) -> str:
    """Lowercase + NFC + gộp khoảng trắng; GIỮ dấu tiếng Việt (dấu đổi nghĩa từ)."""
    lowered = unicodedata.normalize("NFC", text.lower())
    return re.sub(r"\s+", " ", lowered).strip()


def contains_term(haystack: str, term: str) -> bool:
    """Khớp theo ranh giới từ — tránh false positive kiểu 'code' trong 'encode'."""
    escaped = re.escape(term)
    pattern = rf"(?<![{_BOUNDARY}]){escaped}(?![{_BOUNDARY}])"
    return re.search(pattern, haystack) is not None


def tokenize(text: str) -> Set[str]:
    """Tách token đơn (dùng cho chấm điểm retrieval) — giữ nguyên hành vi cũ."""
    return set(_TOKEN_RE.findall(normalize_text(text)))
