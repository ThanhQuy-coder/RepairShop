from fastapi.testclient import TestClient

from app.core.config import settings
from app.main import app

client = TestClient(app)

VALID_PAYLOAD = {
    "requestId": "b3f1c2a0-1234-4a5b-9c1d-abcdef123456",
    "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"},
    "issueDescription": "Pin tụt nhanh",
    "context": {"availableServices": [], "availableParts": []},
}


def test_missing_api_key_header_returns_422_or_401():
    response = client.post("/v1/advisory", json=VALID_PAYLOAD)
    assert response.status_code in (401, 422)


def test_wrong_api_key_returns_401():
    response = client.post(
        "/v1/advisory", json=VALID_PAYLOAD, headers={"X-Internal-Api-Key": "wrong-key-123"}
    )
    assert response.status_code == 401


def test_correct_api_key_passes_authentication():
    response = client.post(
        "/v1/advisory",
        json=VALID_PAYLOAD,
        headers={"X-Internal-Api-Key": settings.internal_api_key},
    )
    # Không assert 200 cứng vì có thể NO_MATCH do context rỗng — chỉ cần xác nhận
    # KHÔNG bị chặn ở bước auth (tức là đã qua được verify_internal_api_key)
    assert response.status_code != 401


def test_health_endpoint_does_not_require_api_key():
    """Health check phải public trong nội bộ mạng — Backend cần gọi được /health TRƯỚC KHI
    biết service có hoạt động hay không, không nên bắt xác thực ở bước kiểm tra sống/chết này."""
    response = client.get("/health")
    assert response.status_code == 200