import pydantic_settings # type: ignore


class Settings(pydantic_settings.BaseSettings):
    app_env: str = "development"
    internal_api_key: str
    llm_provider: str = ""
    llm_base_url: str = "https://api.openai.com/v1"
    llm_api_key: str = ""
    llm_model: str = "gpt-4o-mini"
    request_timeout_seconds: int = 5

    class Config:
        env_file = ".env"


settings = Settings()
