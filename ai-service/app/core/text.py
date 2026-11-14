"""Tiện ích chuẩn hóa văn bản tiếng Việt, từ đồng nghĩa thương hiệu (12 Brands) & model dùng chung cho MAS.

Dùng chung giữa Intent Classifier, Symptom Lexicon, Retrieval Service và Agents
để đảm bảo đồng nhất Unicode NFC, giữ dấu tiếng Việt và matching theo ranh giới từ.
"""

from __future__ import annotations

import re
import unicodedata
from functools import lru_cache
from typing import Dict, Optional, Set, Tuple

_TOKEN_RE = re.compile(r"[0-9a-zA-ZÀ-ỹĐđ]+")
_BOUNDARY = "0-9a-zA-ZÀ-ỹĐđ"

# 12 canonical Brands synced verbatim from BrandAndDeviceModelSeeder.cs
BRAND_SYNONYM_MAP: dict[str, tuple[str, ...]] = {
    "Apple": (
        "apple", "iphone", "ipad", "macbook", "mac book", "apple watch", "airpods",
        "imac", "mac mini", "ip", "mb", "aw", "táo khuyết", "nhà táo",
    ),
    "Samsung": (
        "samsung", "sam sung", "ss", "galaxy", "galaxy s", "galaxy a", "galaxy tab",
        "galaxy watch", "z fold", "z flip", "galaxy note",
    ),
    "Xiaomi": (
        "xiaomi", "mi", "redmi", "redmi note", "xiaomi pad", "poco",
    ),
    "Dell": (
        "dell", "xps", "inspiron", "latitude", "vostro", "precision", "alienware",
    ),
    "ASUS": (
        "asus", "zenbook", "vivobook", "rog", "rog strix", "tuf", "tuf gaming",
    ),
    "Lenovo": (
        "lenovo", "thinkpad", "ideapad", "legion", "yoga",
    ),
    "HP": (
        "hp", "pavilion", "envy", "spectre", "elitebook", "probook", "victus", "omen",
    ),
    "Garmin": (
        "garmin", "forerunner", "fenix", "venu", "instinct",
    ),
    "Sony": (
        "sony", "playstation", "playstation 5", "ps5", "ps4", "dualsense", "wh-1000xm5", "wh1000xm5", "xm5",
    ),
    "Marshall": (
        "marshall", "emberton", "emberton ii", "stanmore", "stanmore iii", "acton", "kilburn",
    ),
    "Nintendo": (
        "nintendo", "nintendo switch", "switch oled", "joy-con", "joycon",
    ),
    "LG": (
        "lg", "ultragear", "xboom", "lg gram", "xboom go",
    ),
}

# Seeded 40 canonical model patterns (longest match checked first)
SEEDED_DEVICE_MODELS: tuple[str, ...] = (
    "Samsung Galaxy S23 Ultra", "Samsung Galaxy S24 Ultra", "Samsung Galaxy A54 5G",
    "iPhone 15 Pro Max", "iPhone 14 Pro", "iPhone 13",
    "Xiaomi Redmi Note 13", "Xiaomi 13T Pro", "Xiaomi 14",
    "MacBook Pro M3 Pro", "MacBook Air M2",
    "Dell XPS 13 9315", "Dell Inspiron 3520",
    "ASUS ROG Strix G16", "ASUS Zenbook 14 OLED",
    "Lenovo ThinkPad T14 Gen 4", "Lenovo Legion 5",
    "HP Pavilion 15", "HP Envy x360 14",
    "iPad Pro 11 M2", "iPad Air 5", "iPad 9",
    "Galaxy Tab S9 Ultra", "Galaxy Tab S9 FE", "Galaxy Tab A9", "Xiaomi Pad 6",
    "Apple Watch Series 9", "Apple Watch Ultra 2",
    "Galaxy Watch 6 Classic", "Galaxy Watch 5 Pro",
    "Garmin Forerunner 265", "Garmin Fenix 7 Pro",
    "PlayStation 5", "Sony WH-1000XM5", "Tay cầm DualSense PS5",
    "Nintendo Switch OLED", "Marshall Emberton II", "Marshall Stanmore III",
    "Loa LG XBOOM Go XG7Q", "Màn hình LG UltraGear 27GP850",
)

# Common Vietnamese SMS/colloquial abbreviations normalized carefully with word boundaries
_COLLOQUIAL_PATTERNS: tuple[tuple[re.Pattern[str], str], ...] = (
    (re.compile(rf"(?<![{_BOUNDARY}])(ko|hok|khg|hem)(?![{_BOUNDARY}])"), "không"),
    (re.compile(rf"(?<![{_BOUNDARY}])k(?=\s+(?:lên|sac|sạc|nhận|nhan|nghe|bat|bắt|vào|vao|len|chay|chạy|hiện|hien))"), "không"),
    (re.compile(rf"(?<![{_BOUNDARY}])(dc|đc|duoc)(?![{_BOUNDARY}])"), "được"),
    (re.compile(rf"(?<![{_BOUNDARY}])(dt|đt)(?![{_BOUNDARY}])"), "điện thoại"),
    (re.compile(rf"(?<![{_BOUNDARY}])mh(?![{_BOUNDARY}])"), "màn hình"),
    (re.compile(rf"(?<![{_BOUNDARY}])(ktra|kt)(?![{_BOUNDARY}])"), "kiểm tra"),
    (re.compile(rf"(?<![{_BOUNDARY}])bh(?![{_BOUNDARY}])"), "bảo hành"),
    (re.compile(rf"(?<![{_BOUNDARY}])bg(?![{_BOUNDARY}])"), "báo giá"),
    (re.compile(rf"(?<![{_BOUNDARY}])(bnh|bn)(?![{_BOUNDARY}])"), "bao nhiêu"),
    (re.compile(rf"(?<![{_BOUNDARY}])vs(?=\s+(?:laptop|máy|may|quạt|quat|macbook))"), "vệ sinh"),
    (re.compile(rf"(?<![{_BOUNDARY}])ip(?=\s*\d{{1,2}})"), "iphone"),
    (re.compile(rf"(?<![{_BOUNDARY}])ss(?![{_BOUNDARY}])"), "samsung"),
)


@lru_cache(maxsize=4096)
def normalize_text(text: str) -> str:
    """Lowercase + Unicode NFC + gộp khoảng trắng; GIỮ nguyên dấu tiếng Việt."""
    if not text:
        return ""
    lowered = unicodedata.normalize("NFC", text.lower())
    return re.sub(r"\s+", " ", lowered).strip()


@lru_cache(maxsize=4096)
def strip_vietnamese_diacritics(text: str) -> str:
    """Bỏ dấu tiếng Việt có kiểm soát để hỗ trợ đối sánh câu không dấu/sai chính tả."""
    if not text:
        return ""
    text_norm = unicodedata.normalize("NFD", text.replace("đ", "d").replace("Đ", "D"))
    stripped = "".join(ch for ch in text_norm if unicodedata.category(ch) != "Mn")
    return re.sub(r"\s+", " ", unicodedata.normalize("NFC", stripped).lower()).strip()


@lru_cache(maxsize=4096)
def normalize_colloquial_vietnamese(text: str) -> str:
    """Chuẩn hóa cơ bản + bung viết tắt phổ biến (ko/k/đc/đt/mh/bh/bg/ktra/ip/ss) giữ đúng ngữ cảnh."""
    base = normalize_text(text)
    if not base:
        return ""
    expanded = base
    for pattern, repl in _COLLOQUIAL_PATTERNS:
        expanded = pattern.sub(repl, expanded)
    return re.sub(r"\s+", " ", expanded).strip()


@lru_cache(maxsize=4096)
def _boundary_regex(term: str) -> re.Pattern[str]:
    escaped = re.escape(term)
    return re.compile(rf"(?<![{_BOUNDARY}]){escaped}(?![{_BOUNDARY}])")


def contains_term(haystack: str, term: str) -> bool:
    """Khớp theo ranh giới từ tiếng Việt — tránh false positive kiểu 'loa' trong 'loạn'."""
    if not haystack or not term:
        return False
    return _boundary_regex(term).search(haystack) is not None


def contains_term_flexible(haystack: str, term: str) -> bool:
    """Khớp cụm có dấu trước; nếu không khớp thì thử khớp dạng bỏ dấu cho đầu vào không dấu."""
    norm_hay = normalize_colloquial_vietnamese(haystack)
    norm_term = normalize_text(term)
    if contains_term(norm_hay, norm_term):
        return True
    no_acc_hay = strip_vietnamese_diacritics(norm_hay)
    no_acc_term = strip_vietnamese_diacritics(norm_term)
    return contains_term(no_acc_hay, no_acc_term)


def tokenize(text: str) -> Set[str]:
    """Tách token đơn (chữ-số tiếng Việt)."""
    return set(_TOKEN_RE.findall(normalize_text(text)))


_SORTED_BRAND_ALIASES: tuple[tuple[str, str, str], ...] = tuple(
    sorted(
        (
            (alias, strip_vietnamese_diacritics(alias), canonical_brand)
            for canonical_brand, aliases in BRAND_SYNONYM_MAP.items()
            for alias in aliases
        ),
        key=lambda item: len(item[0]),
        reverse=True,
    )
)


def normalize_brand(text: Optional[str]) -> Optional[str]:
    """Chuẩn hóa tên thương hiệu/alias về 1 trong 12 Brand chuẩn trong DB (ưu tiên alias dài nhất)."""
    if not text:
        return None
    norm = normalize_colloquial_vietnamese(text)
    no_acc = strip_vietnamese_diacritics(norm)
    for canonical_brand in BRAND_SYNONYM_MAP:
        if contains_term(norm, canonical_brand.lower()):
            return canonical_brand
    for alias, alias_no_acc, canonical_brand in _SORTED_BRAND_ALIASES:
        if contains_term(norm, alias) or contains_term(no_acc, alias_no_acc):
            return canonical_brand
    return None


def extract_brand_and_model(
    text: str,
    hint_brand: Optional[str] = None,
    hint_model: Optional[str] = None,
) -> Tuple[Optional[str], Optional[str]]:
    """Trích xuất (brand, model) chuẩn hóa từ text và metadata thiết bị."""
    resolved_brand = normalize_brand(hint_brand) if hint_brand else None
    hint_combo = f"{hint_brand or ''} {hint_model or ''}".strip()
    combined = f"{hint_combo} {text}".strip()
    norm_combo = normalize_colloquial_vietnamese(hint_combo)
    no_acc_combo = strip_vietnamese_diacritics(norm_combo)
    norm_combined = normalize_colloquial_vietnamese(combined)
    no_acc_combined = strip_vietnamese_diacritics(norm_combined)

    resolved_model: Optional[str] = None
    # First resolve combined brand+model metadata against canonical/series patterns (e.g. "iPhone" + "13" -> "iPhone 13")
    for source_norm, source_no_acc in (
        (norm_combo, no_acc_combo),
        (norm_combined, no_acc_combined),
    ):
        if not source_norm:
            continue
        for model_name in sorted(SEEDED_DEVICE_MODELS, key=len, reverse=True):
            low = model_name.lower()
            if contains_term(source_norm, low) or contains_term(
                source_no_acc, strip_vietnamese_diacritics(low)
            ):
                resolved_model = model_name
                break
        if resolved_model:
            break

    if not resolved_model:
        for source_norm in (norm_combo, norm_combined):
            if not source_norm:
                continue
            m = re.search(
                r"\b(iphone\s+\d{1,2}(?:\s*(?:pro\s*max|pro|plus|mini))?|"
                r"galaxy\s+[sa]\d{1,2}(?:\s*(?:ultra|plus|fe|5g))?|"
                r"redmi\s+note\s+\d{1,2}(?:\s*pro)?|"
                r"macbook\s+(?:air|pro)(?:\s*m[1-4](?:\s*(?:pro|max))?)?|"
                r"ipad\s+(?:pro|air|mini)?\s*\d{0,2}|"
                r"apple\s+watch\s+(?:series\s*\d+|s\d+|ultra\s*\d?)|"
                r"garmin\s+(?:forerunner|fenix|venu)\s*[0-9a-z]+|"
                r"(?:dell\s+)?(?:xps|inspiron|latitude|vostro)\s+\d{1,4}|"
                r"(?:lenovo\s+)?(?:thinkpad|ideapad|legion|yoga)\s+[a-z0-9]+|"
                r"(?:asus\s+)?(?:vivobook|zenbook|rog|tuf)\s+[a-z0-9]+|"
                r"(?:hp\s+)?(?:pavilion|envy|elitebook|probook|victus)\s+[a-z0-9]+)\b",
                source_norm,
                re.IGNORECASE,
            )
            if m:
                resolved_model = m.group(1).strip()
                break

    if not resolved_model and hint_model and hint_model.strip():
        resolved_model = hint_model.strip()

    if not resolved_brand:
        resolved_brand = normalize_brand(combined)

    return resolved_brand, resolved_model


def canonical_brand_aliases(brand: str) -> Dict[str, tuple[str, ...]]:
    """Trả về map brand -> aliases cho test/diagnostics."""
    canonical = normalize_brand(brand)
    if canonical and canonical in BRAND_SYNONYM_MAP:
        return {canonical: BRAND_SYNONYM_MAP[canonical]}
    return {}
