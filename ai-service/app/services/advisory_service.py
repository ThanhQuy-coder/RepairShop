from __future__ import annotations

import logging

from app.llm.llm_client import LLMClient, LLMResponseError, LLMTimeoutError, llm_client
from app.prompts.advisory_prompt import build_user_prompt
from app.prompts.system_prompt import SYSTEM_PROMPT
from app.retrieval.retrieval_service import RetrievalResult, retrieve_candidates
from app.schemas.llm_output_models import LLMAdvisoryOutput
from app.schemas.request_models import AdvisoryRequest
from app.schemas.response_models import (
    AdvisoryResponse,
    ConfidenceLevel,
    PriceRange,
    SuggestedPart,
    SuggestedService,
)

logger = logging.getLogger(__name__)

VALID_CONFIDENCE = {c.value for c in ConfidenceLevel}

_OFF_TOPIC_MARKERS = (
    "thơ",
    "bài hát",
    "code",
    "lập trình",
    "nấu ăn",
    "công thức",
    "dịch thuật",
    "toán",
    "lịch sử",
    "review phim",
    "tình yêu",
    "tư vấn tâm lý",
)


def _looks_off_topic(issue_description: str) -> bool:
    """
    Lưới an toàn THỨ HAI (bên cạnh System Prompt) — nếu mô tả rõ ràng không liên quan sửa chữa
    thiết bị (không có candidate nào match VÀ chứa từ khóa ngoài phạm vi), tự trả OUT_OF_SCOPE
    ngay tại Application, không cần đợi LLM tự nhận diện đúng 100% lần nào cũng vậy.
    """
    lowered = issue_description.lower()
    return any(marker in lowered for marker in _OFF_TOPIC_MARKERS)


class AdvisoryService:
    def __init__(self, client: LLMClient = llm_client) -> None:
        self._llm_client = client

    async def get_advisory(self, request: AdvisoryRequest) -> AdvisoryResponse:

        # Bước 0 — Guard sớm: mô tả rõ ràng ngoài phạm vi, không tốn lượt gọi LLM
        if _looks_off_topic(request.issue_description):
            return AdvisoryResponse.out_of_scope(request.request_id)

        # Bước 1 — Retrieval (Task 6.6): lọc candidate từ context, KHÔNG gọi LLM nếu rỗng
        retrieval = retrieve_candidates(
            request.device, request.issue_description, request.context
        )

        if not retrieval.candidate_services and not retrieval.candidate_parts:
            return AdvisoryResponse.no_match(request.request_id)

        # Bước 2 — Xây prompt từ (User Issue + Retrieved Context), gọi LLM
        user_prompt = build_user_prompt(
            request.device, request.issue_description, retrieval
        )

        try:
            raw_output = await self._llm_client.generate_json(
                SYSTEM_PROMPT, user_prompt
            )
        except LLMTimeoutError:
            logger.warning("LLM timeout for requestId=%s", request.request_id)
            raise
        except LLMResponseError:
            logger.error("LLM error for requestId=%s", request.request_id)
            raise

        llm_output = LLMAdvisoryOutput.model_validate(raw_output)

        # Bước 3 — LLM tự báo out-of-scope (đúng System Prompt Task 6.8)
        if llm_output.out_of_scope:
            return AdvisoryResponse.out_of_scope(request.request_id)

        # Bước 4 — Validate & Map: CHỈ giữ lại service/part mà LLM chọn NẰM TRONG candidate thật.
        # Đây chính là nơi enforce "Không tự tạo service/part không tồn tại" (Task 6.6) bằng code,
        # không dựa hoàn toàn vào việc LLM "nghe lời" prompt.
        matched_services = self._map_services(llm_output, retrieval)
        matched_parts = self._map_parts(llm_output, retrieval)

        if not matched_services and not matched_parts:
            return AdvisoryResponse.no_match(request.request_id)

        price_range = self._build_price_range(llm_output)

        return AdvisoryResponse.success(
            request_id=request.request_id,
            suggested_services=matched_services,
            suggested_parts=matched_parts,
            price_range=price_range,
            reason=llm_output.reason[:300]
            or "AI đã phân tích mô tả tình trạng và đưa ra đề xuất phù hợp.",
        )

    @staticmethod
    def _map_services(
        llm_output: LLMAdvisoryOutput, retrieval: RetrievalResult
    ) -> list[SuggestedService]:
        candidate_by_id = {s.service_id: s for s in retrieval.candidate_services}
        matched: list[SuggestedService] = []

        for item in llm_output.suggested_services:
            candidate = candidate_by_id.get(item.service_id)
            if candidate is None:
                logger.warning(
                    "LLM suggested unknown serviceId=%s — discarded.", item.service_id
                )
                continue  # AI "hallucinate" -> loại bỏ, KHÔNG đưa vào response

            confidence = (
                item.confidence.upper()
                if item.confidence.upper() in VALID_CONFIDENCE
                else "MEDIUM"
            )
            matched.append(
                SuggestedService(
                    serviceId=candidate.service_id,
                    serviceName=candidate.name,  # LẤY TỪ CANDIDATE THẬT, không lấy từ text LLM tự viết
                    confidence=confidence,
                )
            )
        return matched

    @staticmethod
    def _map_parts(
        llm_output: LLMAdvisoryOutput, retrieval: RetrievalResult
    ) -> list[SuggestedPart]:
        candidate_by_id = {p.part_id: p for p in retrieval.candidate_parts}
        matched: list[SuggestedPart] = []

        for item in llm_output.suggested_parts:
            candidate = candidate_by_id.get(item.part_id)
            if candidate is None:
                logger.warning(
                    "LLM suggested unknown partId=%s — discarded.", item.part_id
                )
                continue

            matched.append(
                SuggestedPart(partId=candidate.part_id, partName=candidate.name)
            )
        return matched

    @staticmethod
    def _build_price_range(llm_output: LLMAdvisoryOutput) -> PriceRange:
        """
        Bắt buộc LUÔN là khoảng (Task 6 Tuần 1) — nếu LLM chỉ trả 1 giá hoặc thiếu giá,
        tự suy ra khoảng hợp lý thay vì để None (tránh vi phạm rule "không trả số cố định").
        """
        price_min = llm_output.price_min or 0
        price_max = llm_output.price_max or price_min

        if price_min > price_max:
            price_min, price_max = price_max, price_min

        if price_min == price_max:
            # Nới rộng nhẹ thành khoảng thay vì số cố định — tuân thủ rule "priceRange luôn là khoảng"
            price_max = price_min * 1.2

        return PriceRange(
            min=round(price_min, -3), max=round(price_max, -3)
        )  # làm tròn hàng nghìn


advisory_service = AdvisoryService()
