from __future__ import annotations

from datetime import datetime, timezone
from enum import Enum
from typing import List, Optional
from uuid import UUID

from pydantic import BaseModel, Field # type: ignore


class AdvisoryStatus(str, Enum):
    SUCCESS = "SUCCESS"
    NO_MATCH = "NO_MATCH"
    OUT_OF_SCOPE = "OUT_OF_SCOPE"


class ConfidenceLevel(str, Enum):
    HIGH = "HIGH"
    MEDIUM = "MEDIUM"
    LOW = "LOW"


class SuggestedService(BaseModel):
    """CHỈ chứa serviceId/name lấy từ context.availableServices đã gửi lên — không được tự bịa (Task 6 Tuần 1)."""

    service_id: str = Field(..., alias="serviceId")
    service_name: str = Field(..., alias="serviceName")
    confidence: ConfidenceLevel

    class Config:
        populate_by_name = True


class SuggestedPart(BaseModel):
    part_id: str = Field(..., alias="partId")
    part_name: str = Field(..., alias="partName")

    class Config:
        populate_by_name = True


class PriceRange(BaseModel):
    """Bắt buộc LUÔN là khoảng (min-max) — không được trả số cố định duy nhất (Task 6 Tuần 1)."""

    min: float = Field(..., ge=0)
    max: float = Field(..., ge=0)
    currency: str = "VND"


DEFAULT_DISCLAIMER = (
    "Đây là gợi ý tham khảo từ AI dựa trên dữ liệu cửa hàng. "
    "Giá và dịch vụ chính thức sẽ do nhân viên xác nhận sau khi kiểm tra trực tiếp."
)


class AdvisoryResponse(BaseModel):
    request_id: UUID = Field(..., alias="requestId")
    status: AdvisoryStatus
    suggested_services: List[SuggestedService] = Field(
        default_factory=list, alias="suggestedServices"
    )
    suggested_parts: List[SuggestedPart] = Field(
        default_factory=list, alias="suggestedParts"
    )
    price_range: Optional[PriceRange] = Field(default=None, alias="priceRange")
    reason: str = Field(..., max_length=300)
    disclaimer: Optional[str] = None
    processed_at: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="processedAt"
    )

    class Config:
        populate_by_name = True

    @classmethod
    def success(
        cls,
        request_id: UUID,
        suggested_services: List[SuggestedService],
        suggested_parts: List[SuggestedPart],
        price_range: PriceRange,
        reason: str,
    ) -> "AdvisoryResponse":
        return cls(
            requestId=request_id,
            status=AdvisoryStatus.SUCCESS,
            suggestedServices=suggested_services,
            suggestedParts=suggested_parts,
            priceRange=price_range,
            reason=reason,
            disclaimer=DEFAULT_DISCLAIMER,
        )

    @classmethod
    def no_match(cls, request_id: UUID) -> "AdvisoryResponse":
        return cls(
            requestId=request_id,
            status=AdvisoryStatus.NO_MATCH,
            suggestedServices=[],
            suggestedParts=[],
            priceRange=None,
            reason="Mô tả tình trạng chưa đủ rõ để xác định dịch vụ phù hợp trong danh sách hiện có.",
            disclaimer="Vui lòng liên hệ trực tiếp cửa hàng để được kiểm tra và tư vấn chính xác.",
        )

    @classmethod
    def out_of_scope(cls, request_id: UUID) -> "AdvisoryResponse":
        return cls(
            requestId=request_id,
            status=AdvisoryStatus.OUT_OF_SCOPE,
            suggestedServices=[],
            suggestedParts=[],
            priceRange=None,
            reason="Yêu cầu nằm ngoài phạm vi tư vấn sửa chữa điện thoại/laptop/điện tử.",
            disclaimer=None,
        )
