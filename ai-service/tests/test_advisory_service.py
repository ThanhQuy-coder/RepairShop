import pytest

from app.schemas.request_models import AdvisoryRequest
from app.services.advisory_service import AdvisoryService

VALID_PAYLOAD = {
    "requestId": "b3f1c2a0-1234-4a5b-9c1d-abcdef123456",
    "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"},
    "issueDescription": "Pin tụt nhanh, máy nóng khi sạc",
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
    def __init__(self, response: dict):
        self._response = response

    async def generate_json(self, system_prompt: str, user_prompt: str) -> dict:
        return self._response


@pytest.mark.asyncio
async def test_valid_llm_response_maps_to_success():
    fake = FakeLLMClient(
        {
            "outOfScope": False,
            "suggestedServices": [{"serviceId": "svc-001", "confidence": "HIGH"}],
            "suggestedParts": [{"partId": "part-101"}],
            "priceMin": 300000,
            "priceMax": 500000,
            "reason": "Triệu chứng khớp với pin chai.",
        }
    )
    service = AdvisoryService(client=fake)
    request = AdvisoryRequest.model_validate(VALID_PAYLOAD)

    result = await service.get_advisory(request)

    assert result.status == "SUCCESS"
    assert result.suggested_services[0].service_id == "svc-001"
    assert result.price_range.min == 300000


@pytest.mark.asyncio
async def test_llm_hallucinated_id_is_discarded():
    """Đúng Task 6.6: không tự tạo service/part không tồn tại — kể cả khi LLM 'bịa' ID lạ."""
    fake = FakeLLMClient(
        {
            "outOfScope": False,
            "suggestedServices": [
                {"serviceId": "svc-does-not-exist", "confidence": "HIGH"}
            ],
            "suggestedParts": [],
            "priceMin": 100000,
            "priceMax": 200000,
            "reason": "test",
        }
    )
    service = AdvisoryService(client=fake)
    request = AdvisoryRequest.model_validate(VALID_PAYLOAD)

    result = await service.get_advisory(request)

    assert result.status == "NO_MATCH"  # bị loại hết -> không còn gì hợp lệ -> NO_MATCH


@pytest.mark.asyncio
async def test_out_of_scope_flag_from_llm():
    fake = FakeLLMClient({"outOfScope": True, "reason": "not related"})
    service = AdvisoryService(client=fake)
    request = AdvisoryRequest.model_validate(VALID_PAYLOAD)

    result = await service.get_advisory(request)

    assert result.status == "OUT_OF_SCOPE"
