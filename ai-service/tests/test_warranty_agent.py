"""TASK-10 — Warranty Agent: tra cứu mã BH, chính sách, hết hạn, loại trừ rơi vỡ/nước."""

from __future__ import annotations

import pytest

from app.agents.warranty.agent import WarrantyAgent
from app.schemas.state_models import AgentState


def _policies() -> list[dict]:
    return [
        {
            "packageId": "pkg-6m",
            "name": "Bảo hành Tiêu chuẩn 6 tháng",
            "durationMonths": 6,
            "terms": "Bảo hành lỗi kỹ thuật linh kiện thay thế trong 6 tháng, không áp dụng rơi vỡ hoặc vào nước.",
            "description": "Gói tiêu chuẩn cho pin và màn hình.",
            "isActive": True,
        }
    ]


def _base_state(
    message: str,
    *,
    lookups: list[dict] | None = None,
    policies: list[dict] | None = None,
    warranty_code: str | None = None,
    reference_date: str = "2026-11-09",
) -> AgentState:
    return {
        "request_id": "11111111-1111-1111-1111-111111111111",
        "user_message": message,
        "issue_description": message,
        "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"},
        "warranty_lookups": lookups or [],
        "warranty_policies": _policies() if policies is None else policies,
        "warranty_code": warranty_code,
        "reference_date": reference_date,
        "executed_agents": [],
        "timings": {},
        "errors": [],
    }


@pytest.mark.asyncio
async def test_warranty_active_record_by_code():
    state = _base_state(
        "Kiểm tra mã BH-20261109-001 giúp tôi",
        lookups=[
            {
                "warrantyCode": "BH-20261109-001",
                "ticketCode": "RS-20261109-001",
                "deviceLabel": "iPhone 13",
                "startDate": "2026-09-01",
                "endDate": "2027-03-01",
                "status": "Active",
                "isExpired": False,
                "packageName": "Bảo hành Tiêu chuẩn 6 tháng",
                "terms": "Bảo hành lỗi kỹ thuật 6 tháng",
            }
        ],
    )

    out = await WarrantyAgent().run(state)
    res = out["warranty_result"]

    assert out["need_clarification"] is False
    assert out["warranty_code"] == "BH-20261109-001"
    assert len(res.warranties) == 1
    assert res.warranties[0].status == "Active"
    assert res.warranties[0].is_expired is False
    assert res.requires_verification is True
    assert "còn hiệu lực" in res.explanation


@pytest.mark.asyncio
async def test_warranty_expired_by_end_date():
    state = _base_state(
        "Tra cứu BH-20250101-002",
        lookups=[
            {
                "warrantyCode": "BH-20250101-002",
                "ticketCode": "RS-20250101-002",
                "deviceLabel": "Dell Inspiron 15",
                "startDate": "2025-01-01",
                "endDate": "2026-01-01",
                "status": "Active",
                "isExpired": False,
                "packageName": "Bảo hành Tiêu chuẩn 6 tháng",
                "terms": "6 tháng",
            }
        ],
        reference_date="2026-11-09",
    )

    out = await WarrantyAgent().run(state)
    res = out["warranty_result"]

    assert res.warranties[0].status == "Expired"
    assert res.warranties[0].is_expired is True
    assert "hết hạn" in res.explanation


@pytest.mark.asyncio
async def test_warranty_voided_record():
    state = _base_state(
        "Kiểm tra BH-VOID-001",
        lookups=[
            {
                "warrantyCode": "BH-VOID-001",
                "ticketCode": "RS-20260501-003",
                "deviceLabel": "iPhone 13",
                "startDate": "2026-05-01",
                "endDate": "2027-05-01",
                "status": "Voided",
                "isExpired": False,
                "packageName": None,
                "terms": None,
            }
        ],
    )

    out = await WarrantyAgent().run(state)
    res = out["warranty_result"]

    assert res.warranties[0].status == "Voided"
    assert "vô hiệu lực" in res.explanation or "hủy" in res.explanation


@pytest.mark.asyncio
async def test_warranty_physical_or_water_damage_adds_exclusion_caveat():
    state = _base_state(
        "BH-20261109-001 máy bị rơi vỡ vào nước có được bảo hành không",
        lookups=[
            {
                "warrantyCode": "BH-20261109-001",
                "ticketCode": "RS-20261109-001",
                "deviceLabel": "iPhone 13",
                "startDate": "2026-09-01",
                "endDate": "2027-03-01",
                "status": "Active",
                "isExpired": False,
                "packageName": "Bảo hành Tiêu chuẩn 6 tháng",
                "terms": "Không áp dụng rơi vỡ vào nước",
            }
        ],
    )

    out = await WarrantyAgent().run(state)
    res = out["warranty_result"]

    assert res.requires_verification is True
    assert "rơi vỡ" in res.explanation
    assert "không thuộc phạm vi bảo hành miễn phí" in res.explanation


@pytest.mark.asyncio
async def test_warranty_general_policy_question():
    state = _base_state("Chính sách bảo hành bên mình được bao lâu?")

    out = await WarrantyAgent().run(state)
    res = out["warranty_result"]

    assert out["need_clarification"] is False
    assert len(res.policies) == 1
    assert res.policies[0].duration_months == 6
    assert res.warranties == []
    assert res.requires_verification is True


@pytest.mark.asyncio
async def test_warranty_personal_lookup_missing_code_requests_clarification():
    state = _base_state("Kiểm tra giúp tôi máy tôi còn bảo hành không")

    out = await WarrantyAgent().run(state)
    res = out["warranty_result"]

    assert out["need_clarification"] is True
    assert "warrantyCode" in out["missing_information"]
    assert res.warranties == []
    assert res.policies == []


@pytest.mark.asyncio
async def test_warranty_unknown_code_returns_empty_without_hallucination():
    state = _base_state("Tra cứu bảo hành mã BH-999999")

    out = await WarrantyAgent().run(state)
    res = out["warranty_result"]

    assert out["need_clarification"] is False
    assert res.warranties == []
    assert res.policies == []
    assert "Không tìm thấy" in res.explanation
