"""API cho MAS: `POST /v1/agents/execute` (Task 2.1).

- Xác thực bằng `X-Internal-Api-Key` (Backend gọi nội bộ), không dùng JWT người dùng.
- Import graph theo kiểu LAZY: `/health` và `/v1/advisory` legacy không chịu cold start
  của LangGraph (~2.3s) nếu chưa từng gọi endpoint MAS.
- Lỗi LLM timeout -> 408 AI_TIMEOUT; lỗi không phục hồi -> vẫn trả 200 với status ERROR
  để Backend chủ động fallback thay vì crash.
"""

from __future__ import annotations

import logging

from fastapi import APIRouter, Depends, HTTPException, status  # type: ignore

from app.core.security import verify_internal_api_key
from app.llm.llm_client import LLMResponseError, LLMTimeoutError
from app.schemas.agent_models import AgentRequest, AgentResponse

logger = logging.getLogger(__name__)

router = APIRouter()


@router.post(
    "/agents/execute",
    response_model=AgentResponse,
    response_model_by_alias=True,
    dependencies=[Depends(verify_internal_api_key)],
)
async def execute_agents(payload: AgentRequest) -> AgentResponse:
    # Lazy import: chỉ nạp LangGraph khi thực sự có request MAS.
    from app.orchestration.graph import run_agents

    try:
        return await run_agents(payload)
    except LLMTimeoutError as exc:
        raise HTTPException(
            status_code=status.HTTP_408_REQUEST_TIMEOUT, detail="AI_TIMEOUT"
        ) from exc
    except LLMResponseError as exc:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="AI_INTERNAL_ERROR",
        ) from exc
    except Exception as exc:  # pragma: no cover - lưới an toàn cuối
        logger.exception("Lỗi không mong đợi khi chạy MAS cho request %s", payload.request_id)
        return AgentResponse.error(
            payload.request_id,
            "Không thể xử lý yêu cầu tư vấn lúc này. Vui lòng thử lại sau.",
        )
