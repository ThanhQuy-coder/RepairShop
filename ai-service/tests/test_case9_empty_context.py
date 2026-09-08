import pytest

from app.schemas.request_models import AdvisoryRequest
from app.services.advisory_service import AdvisoryService


@pytest.mark.asyncio
async def test_case9_empty_context_returns_no_match_without_llm_call():
    """availableServices=[] và availableParts=[] -> Retrieval (Task 6.6) trả rỗng ngay,
    KHÔNG gọi LLM (tránh AI phải tự bịa dữ liệu khi không có gì để tham chiếu)."""
    payload = {
        "requestId": "b3f1c2a0-1234-4a5b-9c1d-abcdef123456",
        "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"},
        "issueDescription": "Pin tụt nhanh",
        "context": {"availableServices": [], "availableParts": []},
    }

    class FailingLLMClient:
        """Nếu code gọi tới LLM trong case này là SAI — client này cố tình ném lỗi để phát hiện."""

        async def generate_json(self, system_prompt, user_prompt):
            raise AssertionError(
                "Không được gọi LLM khi context rỗng — Retrieval phải chặn trước."
            )

    service = AdvisoryService(client=FailingLLMClient())
    result = await service.get_advisory(AdvisoryRequest.model_validate(payload))

    assert result.status == "NO_MATCH"
    assert result.suggested_services == []
    assert result.suggested_parts == []
    assert result.price_range is None
