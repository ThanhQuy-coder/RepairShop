import pytest
from pydantic import ValidationError

from app.schemas.request_models import AdvisoryRequest

VALID_PAYLOAD = {
    "requestId": "b3f1c2a0-1234-4a5b-9c1d-abcdef123456",
    "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"},
    "issueDescription": "Pin tụt nhanh",
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


def test_valid_request_parses_successfully():
    req = AdvisoryRequest.model_validate(VALID_PAYLOAD)
    assert str(req.request_id) == VALID_PAYLOAD["requestId"]
    assert req.device.device_type.value == "phone"


def test_invalid_device_type_rejected():
    payload = {
        **VALID_PAYLOAD,
        "device": {**VALID_PAYLOAD["device"], "deviceType": "tablet"},
    }
    with pytest.raises(ValidationError):
        AdvisoryRequest.model_validate(payload)


def test_issue_description_over_500_chars_rejected():
    payload = {**VALID_PAYLOAD, "issueDescription": "a" * 501}
    with pytest.raises(ValidationError):
        AdvisoryRequest.model_validate(payload)


def test_blank_issue_description_rejected():
    payload = {**VALID_PAYLOAD, "issueDescription": "   "}
    with pytest.raises(ValidationError):
        AdvisoryRequest.model_validate(payload)


def test_negative_base_price_rejected():
    payload = {**VALID_PAYLOAD}
    payload["context"]["availableServices"][0]["basePrice"] = -100
    with pytest.raises(ValidationError):
        AdvisoryRequest.model_validate(payload)


def test_empty_context_lists_are_allowed():
    payload = {
        **VALID_PAYLOAD,
        "context": {"availableServices": [], "availableParts": []},
    }
    req = AdvisoryRequest.model_validate(payload)
    assert req.context.available_services == []
