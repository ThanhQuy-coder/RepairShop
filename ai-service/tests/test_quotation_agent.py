"""Task 3.1 — Quotation Agent: giá lấy 100% từ context, không gọi LLM."""

from __future__ import annotations

import pytest

from app.agents.quotation.agent import QuotationAgent
from app.orchestration.graph import _initial_state
from app.schemas.agent_models import (
    AgentRequest,
    DiagnosisResult,
    PossibleCause,
    SuggestedPartOut,
    SuggestedServiceOut,
)
from tests.fixtures.context import CORE_PARTS, CORE_SERVICES, build_agent_payload


def _state(message: str = "Pin tụt nhanh", **kwargs):
    payload = build_agent_payload(message, **kwargs)
    return _initial_state(AgentRequest.model_validate(payload))


def _diagnosis(service_ids=("svc-pin",), part_ids=("part-pin-ip13",)) -> DiagnosisResult:
    return DiagnosisResult(
        possibleCauses=[PossibleCause(name="Pin chai", confidence="HIGH")],
        suggestedServices=[
            SuggestedServiceOut(serviceId=service_id, serviceName="x")
            for service_id in service_ids
        ],
        suggestedParts=[
            SuggestedPartOut(partId=part_id, partName="y") for part_id in part_ids
        ],
        explanation="Pin chai.",
    )


@pytest.mark.asyncio
async def test_quote_uses_context_price_for_service():
    state = _state()
    state["diagnosis_result"] = _diagnosis(part_ids=())

    result = await QuotationAgent().run(state)
    quotation = result["quotation_result"]

    assert len(quotation.items) == 1
    item = quotation.items[0]
    assert item.item_id == "svc-pin"
    assert item.item_type == "SERVICE"
    assert item.unit_price == 400000  # đúng basePrice trong CORE_SERVICES
    assert item.line_total == 400000
    assert quotation.estimated_total == 400000


@pytest.mark.asyncio
async def test_quote_sums_service_and_in_stock_part():
    state = _state()
    state["diagnosis_result"] = _diagnosis()

    result = await QuotationAgent().run(state)
    quotation = result["quotation_result"]

    assert quotation.estimated_total == 400000 + 350000
    assert {item.item_type for item in quotation.items} == {"SERVICE", "PART"}


@pytest.mark.asyncio
async def test_duplicate_diagnosis_ids_are_quoted_once():
    state = _state()
    state["diagnosis_result"] = _diagnosis(service_ids=("svc-pin", "svc-pin"), part_ids=())

    result = await QuotationAgent().run(state)
    quotation = result["quotation_result"]

    assert len(quotation.items) == 1
    assert quotation.estimated_total == 400000


@pytest.mark.asyncio
async def test_out_of_stock_part_is_excluded_with_assumption():
    state = _state()
    state["diagnosis_result"] = _diagnosis(service_ids=(), part_ids=("part-man-hinh-ip13",))

    result = await QuotationAgent().run(state)
    quotation = result["quotation_result"]

    assert quotation.items == []
    assert quotation.estimated_total == 0
    assert any("hết hàng" in note for note in quotation.assumptions)


@pytest.mark.asyncio
async def test_quote_only_intent_selects_services_from_retrieval():
    state = _state("Báo giá giúp tôi dịch vụ thay màn hình", issue_description="")

    result = await QuotationAgent().run(state)
    quotation = result["quotation_result"]

    ids = [item.item_id for item in quotation.items]
    assert "svc-man-hinh" in ids
    assert quotation.estimated_total > 0


@pytest.mark.asyncio
async def test_quotation_always_uses_current_context_price():
    services = [dict(CORE_SERVICES[0], basePrice=999000)]
    state = _state(services=services)
    state["diagnosis_result"] = _diagnosis(part_ids=())

    result = await QuotationAgent().run(state)
    quotation = result["quotation_result"]

    assert quotation.items[0].unit_price == 999000
    assert quotation.estimated_total == 999000


@pytest.mark.asyncio
async def test_empty_context_yields_empty_quotation_with_explanation():
    state = _state(services=[], parts=[])
    state["diagnosis_result"] = _diagnosis(part_ids=())

    result = await QuotationAgent().run(state)
    quotation = result["quotation_result"]

    assert quotation.items == []
    assert quotation.estimated_total == 0
    assert quotation.explanation


@pytest.mark.asyncio
async def test_quotation_never_calls_llm():
    state = _state()
    state["diagnosis_result"] = _diagnosis()

    result = await QuotationAgent().run(state)

    # QuotationAgent không có LLM client và không trả về llm_calls.
    assert "llm_calls" not in result
    assert result["quotation_result"].items


@pytest.mark.asyncio
async def test_quotation_ignores_unknown_ids_from_diagnosis():
    state = _state()
    state["diagnosis_result"] = _diagnosis(
        service_ids=("svc-does-not-exist",), part_ids=("part-fake",)
    )

    result = await QuotationAgent().run(state)

    assert result["quotation_result"].items == []


def test_core_parts_fixture_has_one_out_of_stock_item():
    # Bảo vệ fixture: test out-of-stock chỉ có ý nghĩa khi thật sự có part hết hàng.
    assert any(part["inStock"] is False for part in CORE_PARTS)
