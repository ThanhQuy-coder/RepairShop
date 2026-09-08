import pytest

from app.schemas.request_models import AdvisoryRequest
from app.services.advisory_service import AdvisoryService


def _payload(
    device_type: str, brand: str, model: str, issue: str, services=None, parts=None
):
    return {
        "requestId": "b3f1c2a0-1234-4a5b-9c1d-abcdef123456",
        "device": {"deviceType": device_type, "brand": brand, "model": model},
        "issueDescription": issue,
        "context": {
            "availableServices": services or [],
            "availableParts": parts or [],
        },
    }


class FakeLLMClient:
    def __init__(self, response: dict):
        self._response = response

    async def generate_json(self, system_prompt: str, user_prompt: str) -> dict:
        return self._response


# ───────────────────────── Case 1 — Normal (iPhone 13, pin tụt nhanh) ─────────────────────────


@pytest.mark.asyncio
async def test_case1_normal_iphone_battery_issue_returns_success():
    payload = _payload(
        "phone",
        "iPhone",
        "13",
        "Pin tụt nhanh",
        services=[
            {
                "serviceId": "svc-1",
                "name": "Thay pin",
                "deviceType": "phone",
                "basePrice": 400000,
            }
        ],
        parts=[{"partId": "part-1", "name": "Pin iPhone 13", "unitPrice": 350000}],
    )
    fake_llm = FakeLLMClient(
        {
            "outOfScope": False,
            "suggestedServices": [{"serviceId": "svc-1", "confidence": "HIGH"}],
            "suggestedParts": [{"partId": "part-1"}],
            "priceMin": 300000,
            "priceMax": 500000,
            "reason": "Pin tụt nhanh là dấu hiệu điển hình pin chai.",
        }
    )

    service = AdvisoryService(client=fake_llm)
    result = await service.get_advisory(AdvisoryRequest.model_validate(payload))

    assert result.status == "SUCCESS"


# ───────────────────────── Case 2 — Laptop (nóng + quạt kêu) ─────────────────────────


@pytest.mark.asyncio
async def test_case2_laptop_overheating_returns_service_part_and_price_range():
    payload = _payload(
        "laptop",
        "Dell",
        "XPS 15",
        "Máy nóng, quạt kêu to bất thường",
        services=[
            {
                "serviceId": "svc-2",
                "name": "Vệ sinh tản nhiệt laptop",
                "deviceType": "laptop",
                "basePrice": 250000,
            }
        ],
        parts=[{"partId": "part-2", "name": "Keo tản nhiệt", "unitPrice": 80000}],
    )
    fake_llm = FakeLLMClient(
        {
            "outOfScope": False,
            "suggestedServices": [{"serviceId": "svc-2", "confidence": "HIGH"}],
            "suggestedParts": [{"partId": "part-2"}],
            "priceMin": 250000,
            "priceMax": 400000,
            "reason": "Máy nóng và quạt kêu thường do bụi bẩn/keo tản nhiệt khô.",
        }
    )

    service = AdvisoryService(client=fake_llm)
    result = await service.get_advisory(AdvisoryRequest.model_validate(payload))

    assert result.status == "SUCCESS"
    assert len(result.suggested_services) > 0  # dịch vụ phù hợp
    assert len(result.suggested_parts) > 0  # linh kiện phù hợp
    assert result.price_range is not None  # price range


# ───────────────────────── Case 3 — Out of Scope (viết code C#) ─────────────────────────


@pytest.mark.asyncio
async def test_case3_write_csharp_code_is_out_of_scope():
    payload = _payload(
        "phone", "iPhone", "13", "Viết giúp tôi 1 đoạn code C# sắp xếp mảng"
    )

    # Guard từ khóa off-topic (Task 6.9) đã chặn NGAY, không cần LLM phản hồi đúng
    service = AdvisoryService(client=FakeLLMClient({}))
    result = await service.get_advisory(AdvisoryRequest.model_validate(payload))

    assert result.status == "OUT_OF_SCOPE"


# ───────────────────────── Case 4 — Hallucination (Thay chip ABC Super Pro) ─────────────────────────


@pytest.mark.asyncio
async def test_case4_hallucinated_service_is_discarded_by_ai_service_layer():
    """
    Ở tầng FastAPI (Task 6.7 _map_services), serviceId không nằm trong candidate -> bị loại.
    Đây là lớp phòng thủ THỨ NHẤT. Lớp THỨ HAI (validate lại với PostgreSQL thật) nằm ở
    AIServiceClient.ValidateAndMapAsync() phía .NET — xem AIServiceClientHallucinationTests bên dưới.
    """
    payload = _payload(
        "phone",
        "iPhone",
        "13",
        "Máy chạy chậm, hay treo",
        services=[
            {
                "serviceId": "svc-real",
                "name": "Vệ sinh main",
                "deviceType": "phone",
                "basePrice": 150000,
            }
        ],
    )
    fake_llm = FakeLLMClient(
        {
            "outOfScope": False,
            # LLM "bịa" ra 1 serviceId không hề có trong candidateServices đã gửi
            "suggestedServices": [
                {"serviceId": "svc-fake-chip-abc-super-pro", "confidence": "HIGH"}
            ],
            "suggestedParts": [],
            "priceMin": 500000,
            "priceMax": 800000,
            "reason": "Đề xuất thay chip ABC Super Pro.",
        }
    )

    service = AdvisoryService(client=fake_llm)
    result = await service.get_advisory(AdvisoryRequest.model_validate(payload))

    # serviceId giả không match candidate nào -> _map_services loại bỏ -> không còn suggestion nào -> NO_MATCH
    assert result.status == "NO_MATCH"
    assert all(
        s.service_id != "svc-fake-chip-abc-super-pro" for s in result.suggested_services
    )


# ───────────────────────── Case 5 — Price hallucination ─────────────────────────


@pytest.mark.asyncio
async def test_case5_extreme_price_is_flagged_at_dotnet_layer_not_ai_service():
    """
    Ghi chú quan trọng: FastAPI (AdvisoryService) hiện KHÔNG tự so basePrice vs priceRange —
    trách nhiệm này thuộc về AIServiceClient phía .NET (Task 6.17, AIPriceRangeValidator),
    đúng phân chia "AI Service sinh gợi ý, Backend validate ngược trước khi trả người dùng"
    (Task 5 Tuần 2 mục 6). Test case 5 THẬT SỰ nằm ở AIPriceRangeValidatorTests (.NET),
    đã viết ở Task 6.17. Test dưới đây chỉ xác nhận FastAPI TRẢ ĐÚNG giá trị AI đưa ra
    (không tự làm tròn/sửa), để .NET có dữ liệu gốc mà validate.
    """
    payload = _payload(
        "phone",
        "iPhone",
        "13",
        "Pin tụt nhanh",
        services=[
            {
                "serviceId": "svc-1",
                "name": "Thay pin",
                "deviceType": "phone",
                "basePrice": 400000,
            }
        ],
    )
    fake_llm = FakeLLMClient(
        {
            "outOfScope": False,
            "suggestedServices": [{"serviceId": "svc-1", "confidence": "HIGH"}],
            "suggestedParts": [],
            "priceMin": 100000,
            "priceMax": 50000000,  # giá bất thường AI trả về
            "reason": "test",
        }
    )

    service = AdvisoryService(client=fake_llm)
    result = await service.get_advisory(AdvisoryRequest.model_validate(payload))

    # FastAPI KHÔNG tự chặn — trả nguyên giá trị AI đưa ra để .NET (AIPriceRangeValidator) xử lý
    assert result.status == "SUCCESS"
    assert result.price_range.min == 100000
    assert result.price_range.max == 50000000
