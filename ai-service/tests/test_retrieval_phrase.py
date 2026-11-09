"""Task 2.2 — Retrieval: khớp CỤM tiếng Việt (D-07) và ưu tiên brand/model."""

from __future__ import annotations

from app.retrieval.retrieval_service import retrieve_candidates
from app.schemas.request_models import (
    AdvisoryContext,
    AvailablePart,
    AvailableService,
    DeviceInfo,
    DeviceType,
)
from tests.fixtures.context import CORE_PARTS, CORE_SERVICES


def _context(services=None, parts=None) -> AdvisoryContext:
    return AdvisoryContext(
        availableServices=[
            AvailableService.model_validate(item)
            for item in (services if services is not None else CORE_SERVICES)
        ],
        availableParts=[
            AvailablePart.model_validate(item)
            for item in (parts if parts is not None else CORE_PARTS)
        ],
    )


PHONE = DeviceInfo(deviceType=DeviceType.PHONE, brand="iPhone", model="13")


def test_multi_word_symptom_cam_ung_matches_screen_services():
    result = retrieve_candidates(
        PHONE, "màn hình cảm ứng không nhận", _context()
    )
    ids = [service.service_id for service in result.candidate_services]

    assert "svc-man-hinh" in ids
    assert "svc-cam-ung" in ids
    assert "svc-camera" not in ids  # không liên quan => bị loại


def test_multi_word_symptom_vo_nuoc_matches_cleaning_service():
    result = retrieve_candidates(PHONE, "máy bị vô nước", _context())
    ids = [service.service_id for service in result.candidate_services]

    assert "svc-ve-sinh" in ids


def test_brand_and_model_prioritize_the_right_part():
    parts = [
        {"partId": "part-samsung", "name": "Pin Samsung S23", "unitPrice": 300000},
        {"partId": "part-ip13", "name": "Pin iPhone 13", "unitPrice": 350000},
    ]
    result = retrieve_candidates(
        PHONE, "pin yếu", _context(parts=parts)
    )

    assert result.candidate_parts[0].part_id == "part-ip13"


def test_laptop_service_is_excluded_for_phone_device():
    result = retrieve_candidates(PHONE, "pin tụt nhanh", _context())

    assert all(
        service.device_type == "phone" for service in result.candidate_services
    )
