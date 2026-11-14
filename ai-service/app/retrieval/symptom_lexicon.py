"""Symptom Lexicon chuẩn hóa theo 74 bản ghi DiagnosisSymptomSeeder.cs & mở rộng từ khóa ngữ cảnh.

Hỗ trợ:
- Danh mục đầy đủ 74 triệu chứng chuẩn của Shop theo 5 DeviceType (Phone 15, Laptop 16, Electronics 15, Tablet 14, Smartwatch 14).
- Ánh xạ đồng nghĩa tự nhiên, có dấu, không dấu, viết tắt -> từ khóa linh kiện/dịch vụ.
- Phân biệt ngữ cảnh để tránh ghép nhầm (ví dụ: "chảy mực màn hình" vs "máy in hết mực", "chai pin" vs "chai nước").
"""

from __future__ import annotations

from app.core.text import (
    contains_term,
    contains_term_flexible,
    normalize_colloquial_vietnamese,
    normalize_text,
    strip_vietnamese_diacritics,
)

# 74 canonical DiagnosisSymptoms synchronized verbatim with DiagnosisSymptomSeeder.cs
CANONICAL_DIAGNOSIS_SYMPTOMS: dict[str, tuple[str, ...]] = {
    "phone": (
        "Không lên nguồn", "Pin nhanh hết", "Không sạc được", "Sạc chập chờn",
        "Màn hình bị vỡ/nứt", "Màn hình không hiển thị", "Cảm ứng không hoạt động",
        "Camera lỗi", "Loa/micro không hoạt động", "Không nhận SIM/mất sóng",
        "Wi-Fi/Bluetooth lỗi", "Máy quá nóng", "Nút bấm không hoạt động",
        "Lỗi phần mềm/treo máy", "Vào nước",
    ),
    "laptop": (
        "Không lên nguồn", "Pin chai/không sạc được", "Máy tự tắt", "Máy quá nóng",
        "Quạt tản nhiệt kêu lớn", "Màn hình không hiển thị", "Màn hình bị sọc/nhấp nháy",
        "Bàn phím lỗi", "Touchpad không hoạt động", "Wi-Fi không kết nối",
        "Loa/micro lỗi", "Máy chạy chậm", "Không nhận ổ cứng/SSD", "Không nhận RAM",
        "Lỗi hệ điều hành", "Hư hỏng do chất lỏng",
    ),
    "electronics": (
        "Không lên nguồn", "Nguồn điện không ổn định", "Thiết bị hoạt động chập chờn",
        "Không nhận tín hiệu", "Màn hình không hiển thị", "Nút điều khiển không hoạt động",
        "Loa không phát âm thanh", "Cổng kết nối bị lỗi", "Quá nhiệt",
        "Pin không hoạt động", "Lỗi bo mạch", "Hư hỏng do chất lỏng", "Lỗi phần mềm/firmware",
        "Lỗi mạch nguồn/nguồn phụ", "Nhiễu âm thanh/tín hiệu",
    ),
    "tablet": (
        "Không lên nguồn", "Pin nhanh hết", "Không sạc được", "Màn hình vỡ/nứt",
        "Màn hình không hiển thị", "Cảm ứng lỗi", "Loa/micro lỗi", "Camera lỗi",
        "Wi-Fi/Bluetooth lỗi", "Không nhận SIM/mất sóng", "Máy quá nóng",
        "Lỗi phần mềm/treo máy", "Cổng sạc lỗi", "Hư hỏng do chất lỏng",
    ),
    "smartwatch": (
        "Không lên nguồn", "Pin nhanh hết", "Không sạc được", "Màn hình vỡ/nứt",
        "Màn hình không hiển thị", "Cảm ứng lỗi", "Không kết nối Bluetooth",
        "Không đồng bộ dữ liệu", "GPS không hoạt động", "Cảm biến đo không chính xác",
        "Nút bấm/núm xoay lỗi", "Rung/thông báo không hoạt động",
        "Không nhận SIM/eSIM (nếu hỗ trợ)", "Hư hỏng do chất lỏng",
    ),
}

# Rich keyword expansion table (cụm triệu chứng -> từ khóa dịch vụ / linh kiện)
SYMPTOM_KEYWORD_MAP: dict[str, list[str]] = {
    # Pin / Nguồn / Sạc
    "tụt": ["pin", "thay pin"],
    "tut": ["pin", "thay pin"],
    "pin tụt": ["pin", "thay pin"],
    "pin tut": ["pin", "thay pin"],
    "pin tụt nhanh": ["pin", "thay pin"],
    "pin yếu": ["pin", "thay pin"],
    "pin yeu": ["pin", "thay pin"],
    "tụt pin": ["pin", "thay pin"],
    "tut pin": ["pin", "thay pin"],
    "pin nhanh hết": ["pin", "thay pin"],
    "nhanh hết pin": ["pin", "thay pin"],
    "hao pin": ["pin", "thay pin"],
    "chai": ["pin", "thay pin"],
    "chai pin": ["pin", "thay pin"],
    "pin chai": ["pin", "thay pin"],
    "phồng pin": ["pin", "thay pin"],
    "phong pin": ["pin", "thay pin"],
    "sưng pin": ["pin", "thay pin"],
    "sạc": ["pin", "cáp", "sạc", "chân sạc", "cổng sạc"],
    "sac": ["pin", "sạc", "chân sạc"],
    "không sạc được": ["pin", "chân sạc", "cổng sạc", "ic sạc"],
    "sạc chậm": ["pin", "chân sạc", "củ sạc", "cáp sạc"],
    "sạc chập chờn": ["chân sạc", "cổng sạc", "pin"],
    "chân sạc": ["chân sạc", "cổng sạc", "sửa cổng sạc"],
    "cổng sạc": ["chân sạc", "cổng sạc"],
    "tắt": ["pin", "nguồn", "main"],
    "tắt nguồn": ["nguồn", "main", "pin"],
    "sập nguồn": ["nguồn", "main", "pin"],
    "sap nguon": ["nguồn", "main", "pin"],
    "nguồn": ["main", "nguồn", "pin", "ic nguồn"],
    "không lên nguồn": ["main", "nguồn", "ic nguồn", "pin"],
    "khong len nguon": ["main", "nguồn", "ic nguồn"],
    "mất nguồn": ["nguồn", "main", "ic nguồn"],
    "chết nguồn": ["nguồn", "main", "ic nguồn"],
    "treo logo": ["phần mềm", "main", "chạy lại phần mềm"],
    "bootloop": ["phần mềm", "main"],

    # Màn hình / Kính / Cảm ứng
    "vỡ": ["màn hình", "kính", "mặt kính", "ép kính"],
    "vo man": ["màn hình", "ép kính"],
    "nứt": ["màn hình", "kính", "ép kính"],
    "rạn": ["màn hình", "kính", "ép kính"],
    "rạn chân chim": ["mặt kính", "ép kính", "màn hình"],
    "mặt kính": ["ép kính", "kính", "màn hình"],
    "ép kính": ["ép kính", "kính", "màn hình"],
    "sọc": ["màn hình", "thay màn hình"],
    "sọc xanh": ["màn hình", "thay màn hình"],
    "chảy mực": ["màn hình", "thay màn hình"],
    "đổ mực": ["màn hình", "thay màn hình"],
    "cảm ứng": ["màn hình", "cảm ứng"],
    "liệt cảm ứng": ["màn hình", "cảm ứng"],
    "loạn cảm ứng": ["màn hình", "cảm ứng"],
    "không hiển thị": ["màn hình", "cáp màn hình", "main"],
    "đen màn": ["màn hình", "thay màn hình"],
    "nhấp nháy": ["màn hình", "cáp màn hình"],
    "ám màu": ["màn hình", "thay màn hình"],

    # Nhiệt / Quạt / Vệ sinh / Nước
    "nóng": ["pin", "main", "nguồn", "vệ sinh", "tản nhiệt"],
    "quá nhiệt": ["vệ sinh", "tản nhiệt", "keo tản nhiệt", "quạt"],
    "nong may": ["vệ sinh", "tản nhiệt", "pin"],
    "quạt kêu": ["vệ sinh", "quạt tản nhiệt", "tản nhiệt", "keo tản nhiệt"],
    "quạt": ["vệ sinh", "quạt tản nhiệt"],
    "kêu rè": ["quạt tản nhiệt", "vệ sinh", "loa"],
    "kêu to": ["quạt tản nhiệt", "vệ sinh"],
    "nước": ["main", "vệ sinh", "sấy khô", "vào nước"],
    "vào nước": ["main", "vệ sinh", "sấy khô"],
    "vô nước": ["main", "vệ sinh", "sấy khô"],
    "dính nước": ["main", "vệ sinh"],
    "chất lỏng": ["main", "vệ sinh"],

    # Camera / Âm thanh / Kết nối
    "camera": ["camera", "mô đun camera", "kính camera"],
    "mờ": ["camera", "vệ sinh camera"],
    "rung camera": ["camera"],
    "loa": ["loa", "vệ sinh loa", "màng loa"],
    "rè loa": ["loa", "thay loa"],
    "âm thanh": ["loa", "mic"],
    "nhiễu âm thanh": ["loa", "bo mạch âm thanh", "cổng kết nối"],
    "mic": ["mic", "micro"],
    "micro": ["mic", "micro"],
    "mất sóng": ["ăng ten", "sim", "main", "cột sóng"],
    "không nhận sim": ["khay sim", "ổ sim", "main"],
    "wifi": ["wifi", "ic wifi", "card wifi", "ăng ten"],
    "bluetooth": ["bluetooth", "main", "kết nối"],

    # Laptop / Phần cứng / Phần mềm / Đồng hồ / Electronics
    "bàn phím": ["bàn phím", "thay bàn phím"],
    "liệt phím": ["bàn phím", "thay bàn phím"],
    "touchpad": ["touchpad", "bàn di chuột"],
    "bản lề": ["bản lề", "gia cố bản lề", "vỏ máy"],
    "ổ cứng": ["ssd", "ổ cứng", "nâng cấp ssd"],
    "ssd": ["ssd", "ổ cứng"],
    "ram": ["ram", "nâng cấp ram"],
    "chậm": ["main", "vệ sinh", "phần mềm", "ssd", "ram"],
    "chạy chậm": ["ssd", "ram", "vệ sinh", "phần mềm"],
    "giật lag": ["phần mềm", "ram", "ssd", "vệ sinh"],
    "hệ điều hành": ["cài win", "phần mềm", "hệ điều hành"],
    "treo": ["main", "phần mềm"],
    "firmware": ["phần mềm", "firmware"],
    "bo mạch": ["main", "bo mạch", "sửa main"],
    "cổng kết nối": ["cổng sạc", "cổng hdmi", "cổng usb", "cổng kết nối"],
    "gps": ["gps", "cảm biến", "main"],
    "cảm biến": ["cảm biến", "main"],
    "núm xoay": ["núm xoay", "digital crown", "nút bấm"],
    "tay cầm": ["tay cầm", "analog", "cần gạt", "trôi cần"],
}

# Context disambiguation rules: when an ambiguous trigger matches an exclusionary phrase,
# suppress unrelated service keywords.
_AMBIGUOUS_EXCLUSIONS: dict[str, tuple[str, ...]] = {
    "nước": ("nước ngoài", "nước uống", "uống nước", "lọc nước"),
    "vỡ": ("vỡ nợ", "vỡ kế hoạch"),
    "chai": ("chai nước", "chai lọ"),
    "mờ": ("mờ ám", "mờ nhạt"),
    "treo": ("treo thưởng", "treo giải"),
}

_CANONICAL_LOOKUP_BY_NORM: dict[str, list[str]] = {}
for _dev_type, _names in CANONICAL_DIAGNOSIS_SYMPTOMS.items():
    for _name in _names:
        _norm = normalize_text(_name)
        _CANONICAL_LOOKUP_BY_NORM.setdefault(_norm, []).append(_dev_type)


def get_canonical_symptoms_for_device(device_type: str | None) -> tuple[str, ...]:
    """Trả về danh sách triệu chứng chuẩn theo DeviceType (hoặc gộp nếu chưa rõ thiết bị)."""
    if not device_type:
        ordered: list[str] = []
        seen: set[str] = set()
        for names in CANONICAL_DIAGNOSIS_SYMPTOMS.values():
            for n in names:
                if n not in seen:
                    seen.add(n)
                    ordered.append(n)
        return tuple(ordered)
    return CANONICAL_DIAGNOSIS_SYMPTOMS.get(device_type.lower(), ())


def map_text_to_canonical_symptoms(
    text: str,
    device_type: str | None = None,
) -> list[str]:
    """Ánh xạ văn bản người dùng về tên DiagnosisSymptom chuẩn của Backend nếu khớp rõ."""
    candidates = get_canonical_symptoms_for_device(device_type)
    norm = normalize_colloquial_vietnamese(text)
    no_acc = strip_vietnamese_diacritics(norm)
    matched: list[str] = []

    alias_rules: dict[str, tuple[str, ...]] = {
        "Không lên nguồn": ("không lên nguồn", "khong len nguon", "sập nguồn", "chết nguồn", "bật không lên"),
        "Pin nhanh hết": ("pin tụt", "pin tut", "pin tụt nhanh", "pin tut nhanh", "tụt pin", "hao pin", "pin nhanh hết", "nhanh hết pin", "chai pin", "pin chai"),
        "Pin chai/không sạc được": ("pin tụt", "pin tut", "pin tụt nhanh", "pin tut nhanh", "chai pin", "pin chai", "không sạc được", "pin yếu"),
        "Không sạc được": ("không sạc được", "sạc không vào", "ko nhan sac", "k vào pin"),
        "Sạc chập chờn": ("sạc chập chờn", "lỏng chân sạc", "sạc lúc được lúc không"),
        "Màn hình bị vỡ/nứt": ("vỡ màn hình", "nứt kính", "bể màn", "rạn chân chim"),
        "Màn hình vỡ/nứt": ("vỡ màn hình", "nứt kính", "bể màn", "rạn chân chim"),
        "Màn hình không hiển thị": ("đen màn", "không lên màn", "không hiển thị", "tối thui"),
        "Màn hình bị sọc/nhấp nháy": ("sọc màn", "nhấp nháy", "chảy mực", "sọc xanh"),
        "Cảm ứng không hoạt động": ("liệt cảm ứng", "loạn cảm ứng", "đơ cảm ứng"),
        "Cảm ứng lỗi": ("liệt cảm ứng", "loạn cảm ứng", "đơ cảm ứng", "cảm ứng lỗi"),
        "Quạt tản nhiệt kêu lớn": ("quạt kêu", "kêu rè rè", "kêu to"),
        "Máy quá nóng": ("nóng máy", "quá nhiệt", "nóng ran"),
        "Quá nhiệt": ("nóng máy", "quá nhiệt"),
        "Hư hỏng do chất lỏng": ("rơi nước", "vào nước", "vô nước", "đổ nước", "dính nước"),
        "Vào nước": ("rơi nước", "vào nước", "vô nước", "đổ nước", "dính nước"),
        "Bàn phím lỗi": ("liệt phím", "kẹt phím", "lỗi bàn phím", "hỏng phím"),
        "Máy chạy chậm": ("chậm", "giật lag", "đơ máy"),
        "Loa/micro không hoạt động": ("rè loa", "mất tiếng", "hỏng mic", "loa rè"),
        "Loa/micro lỗi": ("rè loa", "mất tiếng", "hỏng mic", "loa rè"),
        "Loa không phát âm thanh": ("rè loa", "mất tiếng", "không phát âm thanh"),
        "Nhiễu âm thanh/tín hiệu": ("nhiễu âm", "rè", "lẹt xẹt"),
        "Lỗi mạch nguồn/nguồn phụ": ("chập nguồn", "hỏng nguồn phụ", "ic nguồn"),
    }

    for sym in candidates:
        low = normalize_text(sym)
        if contains_term(norm, low) or contains_term(no_acc, strip_vietnamese_diacritics(low)):
            matched.append(sym)
            continue
        for trigger in alias_rules.get(sym, ()):
            if contains_term_flexible(norm, trigger):
                matched.append(sym)
                break
    return matched


def _is_excluded_by_context(normalized: str, key: str) -> bool:
    for blocker in _AMBIGUOUS_EXCLUSIONS.get(key, ()):
        if contains_term(normalized, blocker):
            return True
    return False


def expand_text_keywords(text: str) -> set[str]:
    """
    Mở rộng từ khóa triệu chứng từ cả câu sử dụng ranh giới từ + chống ghép nhầm ngữ cảnh.
    Hỗ trợ đồng thời câu có dấu, không dấu và viết tắt phổ biến.
    """
    normalized = normalize_colloquial_vietnamese(text)
    no_acc = strip_vietnamese_diacritics(normalized)
    expanded: set[str] = set()

    for symptom, related_terms in SYMPTOM_KEYWORD_MAP.items():
        if _is_excluded_by_context(normalized, symptom):
            continue
        matched = contains_term(normalized, symptom) or contains_term(
            no_acc, strip_vietnamese_diacritics(symptom)
        )
        if matched:
            expanded.update(related_terms)
            for term in related_terms:
                expanded.update(term.split())
            expanded.update(symptom.split())
    return expanded


def expand_issue_keywords(issue_tokens: set[str]) -> set[str]:
    """Giữ API cũ: mở rộng từ tập token."""
    return expand_text_keywords(" ".join(sorted(issue_tokens)))
