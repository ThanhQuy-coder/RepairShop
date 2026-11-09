"""Task 3.2 — Parser ngày/giờ tiếng Việt."""

from __future__ import annotations

from datetime import date

import pytest

from app.agents.appointment.datetime_parser import (
    parse_requested_date,
    parse_requested_time,
)

TODAY = date(2026, 11, 9)  # thứ hai


@pytest.mark.parametrize(
    "text,expected",
    [
        ("hôm nay còn chỗ không", date(2026, 11, 9)),
        ("mai 3h chiều nhé", date(2026, 11, 10)),
        ("ngày mai lúc 9h", date(2026, 11, 10)),
        ("ngày kia được không", date(2026, 11, 11)),
        ("mốt tôi qua", date(2026, 11, 11)),
        ("thứ bảy này", date(2026, 11, 14)),
        ("chủ nhật tuần này", date(2026, 11, 15)),
        ("thứ hai tuần sau", date(2026, 11, 16)),
        ("thứ bảy tuần sau", date(2026, 11, 21)),
        ("12/11 lúc 14h", date(2026, 11, 12)),
        ("12-11-2026", date(2026, 11, 12)),
        ("1.12", date(2026, 12, 1)),
        ("còn chỗ nào không", None),
        ("giá bao nhiêu", None),
    ],
)
def test_parse_requested_date(text, expected):
    assert parse_requested_date(text, TODAY) == expected


def test_past_date_without_year_rolls_to_next_year():
    # 1/1 đã qua so với 09/11 => hiểu là 01/01 năm sau.
    assert parse_requested_date("1/1", TODAY) == date(2027, 1, 1)


@pytest.mark.parametrize(
    "text,expected",
    [
        ("9h", (9, 0)),
        ("9 giờ sáng", (9, 0)),
        ("9h30 sáng", (9, 30)),
        ("3h chiều", (15, 0)),
        ("3 giờ chiều", (15, 0)),
        ("3 giờ rưỡi", (3, 30)),
        ("14h", (14, 0)),
        ("14h30", (14, 30)),
        ("10:15", (10, 15)),
        ("12h trưa", (12, 0)),
        ("8h tối", (20, 0)),
        ("12h đêm", (0, 0)),
        ("mấy giờ còn chỗ", None),
        ("ngày mai", None),
    ],
)
def test_parse_requested_time(text, expected):
    preference = parse_requested_time(text)

    if expected is None:
        assert preference is None
    else:
        assert preference is not None
        assert (preference.hour, preference.minute) == expected


def test_date_and_time_together():
    text = "mai 3h chiều còn chỗ không"

    assert parse_requested_date(text, TODAY) == date(2026, 11, 10)
    preference = parse_requested_time(text)
    assert preference is not None
    assert preference.as_hhmm() == "15:00"
