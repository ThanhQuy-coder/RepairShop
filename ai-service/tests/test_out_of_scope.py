import pytest # type: ignore

from app.schemas.request_models import AdvisoryRequest
from app.services.advisory_service import AdvisoryService

BASE_PAYLOAD = {
    "requestId": "b3f1c2a0-1234-4a5b-9c1d-abcdef123456",
    "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"},
    "context": {
        "availableServices": [
            {
                "serviceId": "svc-001",
                "name": "Thay pin",
                "deviceType": "phone",
                "basePrice": 400000,
            }
        ],
        "availableParts": [
            {"partId": "part-101", "name": "Pin iPhone 13", "unitPrice": 350000}
        ],
    },
}


class FakeLLMClient:
    """LLM giả trả lời ĐÚNG theo system prompt — mô phỏng hành vi LLM thật khi nhận input off-topic."""

    def __init__(self, response: dict):
        self._response = response

    async def generate_json(self, system_prompt: str, user_prompt: str) -> dict:
        return self._response


@pytest.mark.asyncio
async def test_poem_request_is_out_of_scope():
    """Input: 'Hãy viết cho tôi một bài thơ.' -> AI: OUT_OF_SCOPE"""
    payload = {**BASE_PAYLOAD, "issueDescription": "Hãy viết cho tôi một bài thơ."}
    request = AdvisoryRequest.model_validate(payload)

    # Guard sớm (mục 1) đã bắt được case này qua marker "thơ" -> không cần LLM
    service = AdvisoryService(client=FakeLLMClient({}))
    result = await service.get_advisory(request)

    assert result.status == "OUT_OF_SCOPE"
    assert result.suggested_services == []
    assert result.suggested_parts == []
    assert result.price_range is None
    assert result.disclaimer is None


@pytest.mark.asyncio
async def test_battery_and_heat_issue_is_success():
    """Input: 'Điện thoại bị nóng và pin tụt nhanh.' -> AI: SUCCESS"""
    payload = {
        **BASE_PAYLOAD,
        "issueDescription": "Điện thoại bị nóng và pin tụt nhanh.",
    }
    request = AdvisoryRequest.model_validate(payload)

    fake_llm = FakeLLMClient(
        {
            "outOfScope": False,
            "suggestedServices": [{"serviceId": "svc-001", "confidence": "HIGH"}],
            "suggestedParts": [{"partId": "part-101"}],
            "priceMin": 300000,
            "priceMax": 500000,
            "reason": "Triệu chứng nóng máy và pin tụt nhanh khớp với dịch vụ Thay pin.",
        }
    )
    service = AdvisoryService(client=fake_llm)
    result = await service.get_advisory(request)

    assert result.status == "SUCCESS"
    assert result.disclaimer is not None
    assert result.suggested_services[0].service_id == "svc-001"


@pytest.mark.asyncio
async def test_llm_explicitly_flags_out_of_scope():
    """Trường hợp LLM tự nhận diện (không qua guard từ khóa) — vẫn phải map đúng OUT_OF_SCOPE."""
    payload = {
        **BASE_PAYLOAD,
        "issueDescription": "Trợ lý ơi, bạn nghĩ gì về tình yêu?",
    }
    request = AdvisoryRequest.model_validate(payload)

    fake_llm = FakeLLMClient(
        {"outOfScope": True, "reason": "Not related to device repair."}
    )
    service = AdvisoryService(client=fake_llm)
    result = await service.get_advisory(request)

    assert result.status == "OUT_OF_SCOPE"
