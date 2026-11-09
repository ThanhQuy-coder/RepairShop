from urllib.parse import urlparse

import pydantic_settings  # type: ignore
from pydantic import field_validator  # type: ignore

# Ollama chạy local — AI Service chỉ được gọi tới các host này.
_ALLOWED_LLM_HOSTS = {"127.0.0.1", "localhost", "0.0.0.0", "::1", "host.docker.internal"}


class Settings(pydantic_settings.BaseSettings):
    app_env: str = "development"
    internal_api_key: str

    # 100% Ollama: mặc định trỏ tới server local, không còn provider đám mây/hosted.
    llm_provider: str = "ollama"
    llm_base_url: str = "http://127.0.0.1:11434/v1"
    llm_api_key: str = "ollama"
    llm_model: str = "qwen2.5:1.5b-instruct-q4_K_M"
    request_timeout_seconds: int = 60
    # ollama | offline. "offline" dùng luật/retrieval, không gọi LLM.
    llm_mode: str = "ollama"

    @field_validator("llm_mode")
    @classmethod
    def validate_mode(cls, value: str) -> str:
        normalized = (value or "ollama").strip().lower()
        if normalized not in {"ollama", "offline"}:
            raise ValueError(
                "LLM_MODE chỉ hỗ trợ 'ollama' hoặc 'offline' — AI Service chỉ dùng Ollama local."
            )
        return normalized

    @field_validator("llm_base_url")
    @classmethod
    def validate_local_llm(cls, value: str) -> str:
        host = (urlparse(value).hostname or "").strip().lower()
        if host not in _ALLOWED_LLM_HOSTS:
            raise ValueError(
                "LLM_BASE_URL phải trỏ tới Ollama local "
                f"({', '.join(sorted(_ALLOWED_LLM_HOSTS))}); nhận được host '{host}'."
            )
        return value

    class Config:
        env_file = ".env"


settings = Settings()
