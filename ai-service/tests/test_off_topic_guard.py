"""Task 2.2 — Off-topic guard (D-01): không chặn nhầm câu sửa chữa hợp lệ."""

from __future__ import annotations

import pytest

from app.schemas.request_models import AdvisoryRequest
from app.services.advisory_service import AdvisoryService, _looks_off_topic
from tests.fixtures.context import build_advisory_payload
from tests.fixtures.llm import FakeLLMClient


@pytest.mark.parametrize(
    "text",
    [
        "Máy hiện lỗi error code 404 khi mở ứng dụng",
        "Bàn phím không gõ được nút thanh toán trên màn hình",
        "Máy đọc mã QR code bị mờ",
        "Điện thoại bị lỗi toán học trong ứng dụng máy tính",
    ],
)
def test_repair_sentences_with_tricky_words_are_in_scope(text):
    assert _looks_off_topic(text) is False


@pytest.mark.parametrize(
    "text",
    [
        "Viết giúp tôi một đoạn code C# sắp xếp mảng",
        "Hãy viết cho tôi một bài thơ về mùa thu",
        "Bạn nghĩ gì về tình yêu",
        "Làm sao để nấu phở ngon",
    ],
)
def test_unrelated_requests_are_off_topic(text):
    assert _looks_off_topic(text) is True


@pytest.mark.asyncio
async def test_error_code_sentence_reaches_llm_and_returns_success():
    request = AdvisoryRequest.model_validate(
        build_advisory_payload("Máy hiện lỗi error code 404 khi mở ứng dụng")
    )
    fake = FakeLLMClient(
        {
            "outOfScope": False,
            "suggestedServices": [{"serviceId": "svc-pin", "confidence": "MEDIUM"}],
            "suggestedParts": [],
            "priceMin": 300000,
            "priceMax": 500000,
            "reason": "Lỗi phần mềm liên quan bộ nhớ.",
        }
    )

    result = await AdvisoryService(client=fake).get_advisory(request)

    assert result.status == "SUCCESS"
    assert fake.calls == 1  # đã thực sự gọi LLM, không bị guard chặn trước


@pytest.mark.asyncio
async def test_poem_is_blocked_before_calling_llm():
    request = AdvisoryRequest.model_validate(
        build_advisory_payload("Hãy viết cho tôi một bài thơ về mùa thu")
    )
    fake = FakeLLMClient({})

    result = await AdvisoryService(client=fake).get_advisory(request)

    assert result.status == "OUT_OF_SCOPE"
    assert fake.calls == 0
