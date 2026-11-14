"""Unit tests for Task 06: Service Matching across accented, unaccented, general & vague queries."""

from __future__ import annotations

from app.retrieval.retrieval_service import (
    is_vague_symptom_description,
    retrieve_candidates,
)
from app.schemas.request_models import (
    AdvisoryContext,
    AvailableService,
    DeviceInfo,
    DeviceType,
)
from tests.fixtures.context import CORE_PARTS, CORE_SERVICES


def _ctx(services=None) -> AdvisoryContext:
    raw = services if services is not None else CORE_SERVICES
    return AdvisoryContext(
        availableServices=[AvailableService.model_validate(s) for s in raw],
        availableParts=[],
    )


def test_service_matching_ranks_top_service_with_normalized_score() -> None:
    device = DeviceInfo(deviceType=DeviceType.PHONE, brand="Apple", model="iPhone 13")
    res = retrieve_candidates(device, "Pin tụt nhanh và sập nguồn", _ctx())
    assert res.matched_any_service is True
    assert res.is_vague_issue is False
    assert res.candidate_services[0].service_id == "svc-pin"
    assert 0.0 < res.service_normalized_scores["svc-pin"] <= 1.0


def test_service_matching_supports_unaccented_vietnamese_and_description() -> None:
    services = [
        {
            "serviceId": "svc-board",
            "name": "Sửa bo mạch chủ",
            "description": "Khắc phục chạm chập tụ nguồn, không lên nguồn, vào nước",
            "deviceType": "electronics",
            "basePrice": 550000,
        },
        {
            "serviceId": "svc-fw",
            "name": "Cập nhật phần mềm firmware",
            "description": "Nạp lại firmware thiết bị điện tử",
            "deviceType": "electronics",
            "basePrice": 200000,
        },
    ]
    device = DeviceInfo(deviceType=DeviceType.ELECTRONICS, brand="Sony", model="PlayStation 5")
    res = retrieve_candidates(device, "may bi vao nuoc khong len nguon", _ctx(services))
    assert res.candidate_services[0].service_id == "svc-board"
    assert res.service_scores["svc-board"] > 0


def test_general_service_is_eligible_when_no_specific_match_beats_it() -> None:
    services = [
        *CORE_SERVICES,
        {
            "serviceId": "svc-kiem-tra-chung",
            "name": "Kiểm tra tổng quát phần cứng",
            "description": "Đo đạc linh kiện, cảm biến, kết nối không dây cho nhiều thiết bị",
            "deviceType": "general",
            "basePrice": 100000,
        },
    ]
    device = DeviceInfo(deviceType=DeviceType.SMARTWATCH, brand="Garmin", model="Garmin Forerunner 265")
    res = retrieve_candidates(device, "Đồng hồ lỗi cảm biến không đồng bộ", _ctx(services))
    assert res.matched_any_service is True
    assert res.candidate_services[0].service_id == "svc-kiem-tra-chung"


def test_vague_description_is_flagged_with_zero_matched_scores() -> None:
    device = DeviceInfo(deviceType=DeviceType.PHONE, brand="Samsung", model="Galaxy S23 Ultra")
    assert is_vague_symptom_description("Máy bị hư cần kiểm tra") is True
    assert is_vague_symptom_description("iPhone bị lỗi rồi") is True
    assert is_vague_symptom_description("Rơi nước mất nguồn") is False

    res = retrieve_candidates(device, "Máy bị hư cần kiểm tra", _ctx())
    assert res.is_vague_issue is True
    assert res.matched_any_service is False
    assert all(score == 0 for score in res.service_scores.values())
