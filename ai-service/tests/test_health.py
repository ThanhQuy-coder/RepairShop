from fastapi.testclient import TestClient

from app.main import app

client = TestClient(app)


def test_health_check_returns_healthy():
    response = client.get("/health")
    assert response.status_code == 200
    assert response.json() == {"status": "healthy"}


def test_advisory_requires_internal_api_key():
    response = client.get("/v1/advisory/ping")
    assert response.status_code in (
        401,
        422,
    )  # 422 nếu thiếu header, 401 nếu sai giá trị


def test_advisory_ping_with_valid_key(monkeypatch):
    from app.core.config import settings

    response = client.get(
        "/v1/advisory/ping",
        headers={"X-Internal-Api-Key": settings.internal_api_key},
    )
    assert response.status_code == 200
