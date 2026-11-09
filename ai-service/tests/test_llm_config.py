"""Task 4.5 (bổ sung) — Bảo đảm AI Service chỉ dùng Ollama local, không còn Gemini."""

from __future__ import annotations

from urllib.parse import urlparse

import pytest
from pydantic import ValidationError

from app.core.config import Settings, settings


def test_default_settings_point_to_local_ollama():
    assert settings.llm_mode == "ollama"
    assert settings.llm_provider == "ollama"
    host = urlparse(settings.llm_base_url).hostname
    assert host in {"127.0.0.1", "localhost", "::1"}


def test_no_gemini_reference_in_runtime_configuration():
    for value in (
        settings.llm_provider,
        settings.llm_base_url,
        settings.llm_model,
        settings.llm_api_key,
    ):
        assert "google" not in value.lower()
        assert "gemini" not in value.lower()


def test_settings_reject_remote_gemini_url():
    with pytest.raises(ValidationError):
        Settings(
            internal_api_key="test",
            llm_base_url="https://generativelanguage.googleapis.com/v1beta/openai",
        )


def test_settings_reject_unknown_mode():
    with pytest.raises(ValidationError):
        Settings(internal_api_key="test", llm_mode="hosted")


def test_settings_accept_offline_mode():
    offline = Settings(internal_api_key="test", llm_mode="offline")
    assert offline.llm_mode == "offline"
