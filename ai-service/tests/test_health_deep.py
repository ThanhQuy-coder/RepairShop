"""Task 1.3 — test health check chi tiết `GET /health/deep`.

Nguyên tắc:
- Không gọi mạng trong test (probe LLM chỉ chạy khi `?probe=true` và mode != offline).
- Không phá hợp đồng cũ: `GET /health` vẫn trả đúng `{"status": "healthy"}`.
"""

from fastapi.testclient import TestClient

from app.core import health
from app.core.config import settings
from app.main import app

client = TestClient(app)


def test_health_endpoint_is_unchanged():
    """GET /health giữ nguyên contract cũ mà Backend đang dùng."""
    response = client.get("/health")
    assert response.status_code == 200
    assert response.json() == {"status": "healthy"}


def test_health_deep_returns_all_components_without_api_key():
    """ /health/deep là endpoint vận hành nội bộ: không yêu cầu X-Internal-Api-Key. """
    response = client.get("/health/deep")
    assert response.status_code == 200
    body = response.json()

    assert body["status"] in {"ok", "degraded"}
    assert set(body["components"]) == {"llm", "mas", "classifier"}
    assert body["components"]["mas"]["langgraphAvailable"] is True
    assert body["components"]["classifier"]["sklearnAvailable"] is True
    assert isinstance(body["uptimeSeconds"], (int, float))

    versions = body["versions"]
    for key in ("python", "langgraph", "scikit-learn", "fastapi", "pydantic", "httpx"):
        assert key in versions
    assert versions["python"].startswith("3.")


def test_health_deep_does_not_leak_secrets():
    """Không được để lộ internal API key hoặc LLM API key thật trong payload health."""
    response = client.get("/health/deep")
    text = response.text

    assert settings.internal_api_key not in text
    # Ollama local dùng key quy ước "ollama" (không phải bí mật) nên chỉ kiểm tra key thật.
    if settings.llm_api_key and settings.llm_api_key.lower() != "ollama":
        assert settings.llm_api_key not in text


def test_health_deep_does_not_probe_llm_by_default():
    """Mặc định không gọi mạng: reachable=None, có ghi chú hướng dẫn dùng ?probe=true."""
    body = client.get("/health/deep").json()
    llm = body["components"]["llm"]

    assert llm["reachable"] is None
    assert "probe" in (llm["detail"] or "")


def test_health_deep_offline_mode_is_not_degraded(monkeypatch):
    """LLM_MODE=offline là lựa chọn có chủ đích, không được coi là service suy giảm."""
    monkeypatch.setattr(settings, "llm_mode", "offline")

    body = client.get("/health/deep?probe=true").json()

    assert body["components"]["llm"]["mode"] == "offline"
    assert body["components"]["llm"]["reachable"] is False
    assert body["status"] == "ok"


def test_health_deep_degrades_when_langgraph_missing(monkeypatch):
    """Thiếu LangGraph -> status degraded + nêu rõ lý do (phục vụ chẩn đoán môi trường)."""
    monkeypatch.setattr(health, "_langgraph_available", lambda: False)

    body = client.get("/health/deep").json()

    assert body["status"] == "degraded"
    assert body["components"]["mas"]["langgraphAvailable"] is False
    assert "langgraph" in body["components"]["mas"]["detail"]


def test_health_deep_degrades_when_ollama_not_configured(monkeypatch):
    """Mode ollama nhưng thiếu LLM_API_KEY -> degraded, phát hiện sớm trước khi demo."""
    monkeypatch.setattr(settings, "llm_mode", "ollama")
    monkeypatch.setattr(settings, "llm_api_key", "")

    body = client.get("/health/deep").json()

    assert body["components"]["llm"]["configured"] is False
    assert body["status"] == "degraded"


def test_health_deep_reports_classifier_state_consistently():
    """Classifier phản ánh đúng sự tồn tại của model intent (Task 2.1 đã train model)."""
    classifier = client.get("/health/deep").json()["components"]["classifier"]
    model_exists = health._INTENT_MODEL_PATH.is_file()

    assert classifier["loaded"] is model_exists
    assert classifier["fallback"] == (None if model_exists else "rule")


def test_health_deep_reports_compiled_graph_after_task_2_1():
    """Sau Task 2.1, graph MAS đã tồn tại => health/deep phải thấy graphCompiled=true."""
    mas = client.get("/health/deep").json()["components"]["mas"]

    assert mas["langgraphAvailable"] is True
    assert mas["graphCompiled"] is True
    assert mas["detail"] is None
