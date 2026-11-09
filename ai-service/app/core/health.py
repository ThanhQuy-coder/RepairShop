"""
Health check chi tiết cho AI Service — Task 1.3 (Day 1).

Mục đích:
- Cung cấp `GET /health/deep` để biết service "sống" nhưng thành phần nào đang thiếu
  (LangGraph, scikit-learn, model intent, LLM provider) — phục vụ MAS ở các Task sau.
- KHÔNG thay đổi `GET /health` hiện có (Backend đang dùng để kiểm tra service sống).

Nguyên tắc:
- Endpoint luôn trả 200 khi tiến trình còn phục vụ được; tình trạng thiếu thành phần
  được phản ánh qua `status` = "ok" | "degraded", không phải HTTP error.
- Mặc định KHÔNG gọi mạng (health phải nhanh); chỉ probe LLM khi gọi `?probe=true`,
  và kết quả được cache 30 giây để không bắn request liên tục.
- Không trả về API key, connection string hay bất kỳ bí mật nào.
"""

from __future__ import annotations

import importlib
import importlib.metadata as metadata
import importlib.util
import logging
import sys
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

import httpx  # type: ignore

from app.core.config import settings

logger = logging.getLogger(__name__)

_STARTED_AT = time.monotonic()

_PROBE_CACHE_TTL_SECONDS = 30.0
_PROBE_TIMEOUT_SECONDS = 2.0
_probe_cache: dict[str, Any] | None = None
_probe_cached_at: float = 0.0

MAS_GRAPH_MODULE = "app.orchestration.graph"
_INTENT_MODEL_PATH = (
    Path(__file__).resolve().parents[1] / "ml" / "models" / "intent_clf.joblib"
)


def _now_iso() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


def _version(package: str) -> str | None:
    """Version của package đã cài, None nếu chưa cài (không raise)."""
    try:
        return metadata.version(package)
    except metadata.PackageNotFoundError:
        return None


def _module_available(module: str) -> bool:
    """Kiểm tra module import được mà KHÔNG thực sự import (tránh side effect)."""
    try:
        return importlib.util.find_spec(module) is not None
    except (ImportError, ValueError):
        return False


def _langgraph_available() -> bool:
    return _module_available("langgraph.graph")


def _mas_component() -> dict[str, Any]:
    """
    Trạng thái lớp orchestration. Ở Task 1.3, graph chưa tồn tại (dự kiến Task 2.1)
    nên `graphCompiled=false` là bình thường, KHÔNG phải lỗi.
    """
    component: dict[str, Any] = {
        "langgraphAvailable": _langgraph_available(),
        "graphCompiled": False,
        "detail": None,
    }

    if not component["langgraphAvailable"]:
        component["detail"] = "langgraph chưa được cài (xem requirements-mas.txt)"
        return component

    try:
        module = importlib.import_module(MAS_GRAPH_MODULE)
    except ModuleNotFoundError:
        component["detail"] = "graph chưa được dựng (dự kiến ở Task 2.1)"
        return component
    except Exception as exc:  # pragma: no cover - phòng lỗi import bất ngờ
        component["detail"] = f"graph import lỗi: {type(exc).__name__}"
        logger.warning("Không import được %s: %s", MAS_GRAPH_MODULE, exc)
        return component

    component["graphCompiled"] = getattr(module, "compiled_graph", None) is not None
    if not component["graphCompiled"]:
        component["detail"] = "module graph tồn tại nhưng chưa expose compiled_graph"
    return component


def _classifier_component() -> dict[str, Any]:
    sklearn_available = _module_available("sklearn")
    loaded = sklearn_available and _INTENT_MODEL_PATH.is_file()
    return {
        "sklearnAvailable": sklearn_available,
        "loaded": loaded,
        "fallback": None if loaded else "rule",
    }


def _probe_llm() -> bool:
    """Gọi thử endpoint OpenAI-compatible; chỉ coi là 'unreachable' khi lỗi mạng/5xx."""
    global _probe_cache, _probe_cached_at

    now = time.monotonic()
    if (
        _probe_cache is not None
        and now - _probe_cached_at < _PROBE_CACHE_TTL_SECONDS
    ):
        return bool(_probe_cache.get("reachable"))

    headers: dict[str, str] = {}
    if settings.llm_api_key:
        headers["Authorization"] = f"Bearer {settings.llm_api_key}"

    reachable = False
    try:
        with httpx.Client(timeout=_PROBE_TIMEOUT_SECONDS) as client:
            response = client.get(
                f"{settings.llm_base_url.rstrip('/')}/models", headers=headers
            )
        # 4xx vẫn chứng minh kết nối được tới provider (VD endpoint /models không hỗ trợ).
        reachable = response.status_code < 500
    except httpx.HTTPError as exc:
        logger.info("LLM probe thất bại: %s", type(exc).__name__)

    _probe_cache = {"reachable": reachable}
    _probe_cached_at = now
    return reachable


def _llm_component(probe: bool) -> dict[str, Any]:
    mode = (settings.llm_mode or "ollama").strip().lower()
    component: dict[str, Any] = {
        "mode": mode,
        "configured": True if mode == "offline" else bool(settings.llm_api_key),
        "model": settings.llm_model or None,
        "reachable": None,
    }

    if mode == "offline":
        component["reachable"] = False
        component["detail"] = "offline mode: chủ động không gọi mạng"
        return component

    if not probe:
        component["detail"] = "chưa probe (gọi ?probe=true để kiểm tra kết nối)"
        return component

    component["reachable"] = _probe_llm()
    return component


def build_health_report(probe: bool = False) -> dict[str, Any]:
    """Dựng payload cho `GET /health/deep` (không có side effect ngoài probe LLM)."""
    mas = _mas_component()
    classifier = _classifier_component()
    llm = _llm_component(probe)

    degraded = (
        not mas["langgraphAvailable"]
        or not classifier["sklearnAvailable"]
        or not llm["configured"]
        or (llm["mode"] != "offline" and llm["reachable"] is False)
    )

    return {
        "status": "degraded" if degraded else "ok",
        "service": "repairshop-ai-service",
        "appEnv": settings.app_env,
        "checkedAt": _now_iso(),
        "uptimeSeconds": round(time.monotonic() - _STARTED_AT, 3),
        "components": {
            "llm": llm,
            "mas": mas,
            "classifier": classifier,
        },
        "versions": {
            "python": sys.version.split()[0],
            "langgraph": _version("langgraph"),
            "scikit-learn": _version("scikit-learn"),
            "fastapi": _version("fastapi"),
            "pydantic": _version("pydantic"),
            "httpx": _version("httpx"),
        },
    }
