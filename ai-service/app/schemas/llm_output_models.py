from __future__ import annotations

from typing import List, Optional

from pydantic import BaseModel, Field  # type: ignore


class LLMSuggestedService(BaseModel):
    """serviceId LLM chọn — PHẢI được validate lại có nằm trong candidate list hay không (Task 6.8/6.9)."""

    service_id: str = Field(..., alias="serviceId")
    confidence: (
        str  # "HIGH" | "MEDIUM" | "LOW" — validate strict hơn ở advisory_service
    )

    class Config:
        populate_by_name = True


class LLMSuggestedPart(BaseModel):
    part_id: str = Field(..., alias="partId")

    class Config:
        populate_by_name = True


class LLMAdvisoryOutput(BaseModel):
    """Cấu trúc JSON mà System Prompt (Task 6.8) yêu cầu LLM phải trả về."""

    out_of_scope: bool = Field(default=False, alias="outOfScope")
    suggested_services: List[LLMSuggestedService] = Field(
        default_factory=list, alias="suggestedServices"
    )
    suggested_parts: List[LLMSuggestedPart] = Field(
        default_factory=list, alias="suggestedParts"
    )
    price_min: Optional[float] = Field(default=None, alias="priceMin")
    price_max: Optional[float] = Field(default=None, alias="priceMax")
    reason: str = ""

    class Config:
        populate_by_name = True
