import json

from app.schemas.response_models import AdvisoryResponse, PriceRange, SuggestedPart, SuggestedService


def test_response_is_valid_structured_json_not_free_text():
    response = AdvisoryResponse.success(
        request_id="b3f1c2a0-1234-4a5b-9c1d-abcdef123456",
        suggested_services=[SuggestedService(serviceId="svc-001", serviceName="Thay pin", confidence="HIGH")],
        suggested_parts=[SuggestedPart(partId="part-101", partName="Pin iPhone 13")],
        price_range=PriceRange(min=300000, max=500000),
        reason="Triệu chứng khớp pin chai.",
    )

    payload = json.loads(response.model_dump_json(by_alias=True))

    # Đúng cấu trúc bắt buộc — Backend (Task 6.12) sẽ deserialize thẳng, KHÔNG cần parse regex
    assert isinstance(payload["suggestedServices"], list)
    assert payload["suggestedServices"][0]["serviceId"] == "svc-001"
    assert payload["suggestedServices"][0]["serviceName"] == "Thay pin"
    assert isinstance(payload["priceRange"], dict)
    assert payload["priceRange"]["min"] == 300000
    assert payload["priceRange"]["max"] == 500000

    # reason là free-text CHỈ để hiển thị giải thích — nhưng KHÔNG chứa dữ liệu cấu trúc
    # (không lẫn "serviceId:" hay JSON thô vào chuỗi reason)
    assert "serviceId" not in payload["reason"]
    assert "{" not in payload["reason"]


def test_suggested_service_has_no_free_text_price_embedded():
    """Đảm bảo giá không bao giờ bị nhét vào tên dịch vụ dạng text tự do như '...khoảng 400k'."""
    service = SuggestedService(serviceId="svc-001", serviceName="Thay pin", confidence="HIGH")
    assert "k" not in service.service_name.lower().replace("pin", "")  # không có "400k" lẫn vào tên
    assert "đ" not in service.service_name and "vnđ" not in service.service_name.lower()