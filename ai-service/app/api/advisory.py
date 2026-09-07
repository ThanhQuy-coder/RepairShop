from uuid import UUID

from fastapi import APIRouter, Depends, HTTPException, status # type: ignore

from app.core.security import verify_internal_api_key
from app.retrieval.retrieval_service import retrieve_candidates
from app.schemas.request_models import AdvisoryRequest
from app.schemas.response_models import AdvisoryResponse

from app.llm.llm_client import LLMResponseError, LLMTimeoutError
from app.services.advisory_service import advisory_service

router = APIRouter()


@router.get("/advisory/ping", dependencies=[Depends(verify_internal_api_key)])
def advisory_ping():
    return {"message": "Advisory router is wired correctly."}


@router.post(
    "/advisory",
    response_model=AdvisoryResponse,
    response_model_by_alias=True,
    dependencies=[Depends(verify_internal_api_key)],
)
async def create_advisory(payload: AdvisoryRequest) -> AdvisoryResponse:
    try:
        return await advisory_service.get_advisory(payload)
    except LLMTimeoutError as exc:
        # Khớp mã lỗi 408 AI_TIMEOUT đã thiết kế ở Task 5 Tuần 2 (mục Xử lý lỗi)
        raise HTTPException(
            status_code=status.HTTP_408_REQUEST_TIMEOUT, detail="AI_TIMEOUT"
        ) from exc
    except LLMResponseError as exc:
        # Khớp mã lỗi 500 AI_INTERNAL_ERROR
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="AI_INTERNAL_ERROR",
        ) from exc
