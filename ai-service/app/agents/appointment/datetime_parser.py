"""
Bộ phân tích ngày/giờ tiếng Việt cho Appointment Agent (Task 3.2).

Phạm vi hỗ trợ (đủ cho demo, không cần NLP nặng):
- Ngày tuyệt đối: "12/11", "12-11-2026", "1.12"
- Ngày tương đối: "hôm nay", "mai"/"ngày mai", "ngày kia"/"mốt"
- Thứ trong tuần: "thứ hai".."chủ nhật", "thứ 2".."thứ 7", "cn"; hỗ trợ "tuần sau"
- Giờ: "9h", "9 giờ", "9:30", "14h", "3h chiều", "8h tối", "12h trưa", "9h30 sáng", "3 giờ rưỡi"

Nguyên tắc: KHÔNG đoán bừa. Nếu không xác định được => trả None để agent hỏi lại.
"""

from __future__ import annotations

import re
from dataclasses import dataclass
from datetime import date, timedelta
from typing import Optional

from app.core.text import normalize_text

_DMY_PATTERN = re.compile(
    r"(?<!\d)(?P<day>\d{1,2})\s*[/\-.]\s*(?P<month>\d{1,2})"
    r"(?:\s*[/\-.]\s*(?P<year>\d{2,4}))?(?!\d)"
)
_TIME_PATTERN = re.compile(
    r"(?<![\d/:])(?P<hour>\d{1,2})\s*(?:h|giờ|:)\s*(?P<minute>\d{1,2})?(?![\d/])"
)

# Thứ trong tuần: Python weekday() = 0 (thứ hai) .. 6 (chủ nhật)
_WEEKDAY_TERMS: tuple[tuple[str, int], ...] = (
    ("chủ nhật", 6),
    ("thứ hai", 0),
    ("thứ ba", 1),
    ("thứ tư", 2),
    ("thứ năm", 3),
    ("thứ sáu", 4),
    ("thứ bảy", 5),
    ("thứ 2", 0),
    ("thứ 3", 1),
    ("thứ 4", 2),
    ("thứ 5", 3),
    ("thứ 6", 4),
    ("thứ 7", 5),
    ("cn", 6),
)

_PM_MARKERS = ("chiều", "tối", "đêm")
_AM_MARKERS = ("sáng",)
_NOON_MARKERS = ("trưa",)


@dataclass(frozen=True)
class TimePreference:
    hour: int
    minute: int

    def as_hhmm(self) -> str:
        return f"{self.hour:02d}:{self.minute:02d}"


def _contains_word(text: str, term: str) -> bool:
    pattern = rf"(?<![0-9a-zA-ZÀ-ỹĐđ]){re.escape(term)}(?![0-9a-zA-ZÀ-ỹĐđ])"
    return re.search(pattern, text) is not None


def parse_requested_date(text: str, today: date) -> Optional[date]:
    """Trả về ngày người dùng muốn hẹn, hoặc None nếu câu không nêu ngày."""
    normalized = normalize_text(text)

    # 1) Ngày tuyệt đối (ưu tiên cao nhất vì rõ ràng nhất)
    match = _DMY_PATTERN.search(normalized)
    if match:
        day = int(match.group("day"))
        month = int(match.group("month"))
        year_text = match.group("year")
        if year_text:
            year = int(year_text)
            if year < 100:
                year += 2000
        else:
            year = today.year
        try:
            candidate = date(year, month, day)
        except ValueError:
            return None
        if year_text is None and candidate < today:
            try:
                candidate = date(year + 1, month, day)
            except ValueError:
                return None
        return candidate

    # 2) Ngày tương đối
    if _contains_word(normalized, "hôm qua"):
        return today - timedelta(days=1)
    if _contains_word(normalized, "hôm nay") or _contains_word(normalized, "nay"):
        return today
    if _contains_word(normalized, "ngày kia") or _contains_word(normalized, "mốt"):
        return today + timedelta(days=2)
    if _contains_word(normalized, "ngày mai") or _contains_word(normalized, "mai"):
        return today + timedelta(days=1)

    # 3) Thứ trong tuần
    for term, weekday in _WEEKDAY_TERMS:
        if _contains_word(normalized, term):
            if "tuần sau" in normalized:
                # "thứ X tuần sau" = ngày X của TUẦN KẾ TIẾP (tuần bắt đầu từ thứ hai).
                days_to_next_monday = 7 - today.weekday()
                delta = days_to_next_monday + weekday
            else:
                delta = (weekday - today.weekday()) % 7
                if delta == 0:
                    delta = 7  # hôm nay đúng thứ đó => hiểu là tuần sau
            return today + timedelta(days=delta)

    return None


def parse_requested_time(text: str) -> Optional[TimePreference]:
    """Trả về giờ người dùng muốn hẹn (24h), hoặc None nếu không nêu giờ."""
    normalized = normalize_text(text)
    match = _TIME_PATTERN.search(normalized)
    if not match:
        return None

    hour = int(match.group("hour"))
    minute = int(match.group("minute") or 0)
    tail = normalized[match.end() : match.end() + 14]

    if any(marker in tail for marker in _PM_MARKERS):
        if hour < 12:
            hour += 12
        elif hour == 12 and "đêm" in tail:
            hour = 0
    elif any(marker in tail for marker in _NOON_MARKERS):
        if hour < 12:
            hour += 12
    elif any(marker in tail for marker in _AM_MARKERS):
        if hour == 12:
            hour = 0

    if "rưỡi" in tail:
        minute += 30

    if minute >= 60:
        hour += minute // 60
        minute = minute % 60

    if not (0 <= hour <= 23 and 0 <= minute <= 59):
        return None
    return TimePreference(hour=hour, minute=minute)
