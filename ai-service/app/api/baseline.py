"""API cho Single-Agent baseline: `POST /v1/baseline/execute` (Task 4.2).

Cùng auth, cùng schema request/response với MAS để phép so sánh là công bằng.
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
    "/baseline/execute",
    response_model=AgentResponse,
    response_model_by_alias=True,
    dependencies=[Depends(verify_internal_api_key)],
)
async def execute_baseline(payload: AgentRequest) -> AgentResponse:
    from app.baseline.single_agent import run_baseline

    try:
        return await run_baseline(payload)
    except LLMTimeoutError as exc:
        raise HTTPException(
            status_code=status.HTTP_408_REQUEST_TIMEOUT, detail="AI_TIMEOUT"
        ) from exc
    except LLMResponseError as exc:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="AI_INTERNAL_ERROR",
        ) from exc
    except Exception:  # pragma: no cover - lưới an toàn cuối
        logger.exception("Lỗi không mong đợi ở baseline cho request %s", payload.request_id)
        return AgentResponse.error(
            payload.request_id,
            "Không thể xử lý yêu cầu tư vấn lúc này. Vui lòng thử lại sau.",
        )
