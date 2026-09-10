import pydantic_settings  # type: ignore


class Settings(pydantic_settings.BaseSettings):
    app_env: str = "development"
    internal_api_key: str
    llm_provider: str = ""
    llm_base_url: str = "https://generativelanguage.googleapis.com/v1beta/openai"
    llm_api_key: str = ""
    llm_model: str = "gemini-2.5-flash"
    request_timeout_seconds: int = 30

    class Config:
        env_file = ".env"


settings = Settings()
