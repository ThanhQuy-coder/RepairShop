from __future__ import annotations

from typing import List, Optional

from pydantic import BaseModel, Field, field_validator  # type: ignore


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


# ───────────── Task 2.2 — output riêng cho Diagnosis Agent ─────────────


class LLMPossibleCause(BaseModel):
    name: str = Field(..., min_length=1, max_length=200)
    confidence: str = "MEDIUM"

    class Config:
        populate_by_name = True


class LLMDiagnosisOutput(BaseModel):
    """
    Hợp đồng JSON của Diagnosis Agent. LLM CHỈ được chọn ID trong candidate đã gửi;
    tuyệt đối không sinh giá, không kết luận chắc chắn, không tạo ID mới.
    """

    out_of_scope: bool = Field(default=False, alias="outOfScope")
    possible_causes: List[LLMPossibleCause] = Field(
        default_factory=list, alias="possibleCauses"
    )
    suggested_service_ids: List[str] = Field(
        default_factory=list, alias="suggestedServiceIds"
    )
    suggested_part_ids: List[str] = Field(
        default_factory=list, alias="suggestedPartIds"
    )
    missing_information: List[str] = Field(
        default_factory=list, alias="missingInformation"
    )
    explanation: str = ""

    @field_validator(
        "suggested_service_ids",
        "suggested_part_ids",
        "missing_information",
        mode="before",
    )
    @classmethod
    def coerce_string_to_list(cls, value: object) -> object:
        """
        Model nhỏ (Ollama 1.5B) hay trả 1 string thay vì list. Đây là lỗi schema nhẹ,
        không ảnh hưởng nghiệp vụ => chuẩn hoá thay vì đánh fail cả request.
        """
        if value is None:
            return []
        if isinstance(value, str):
            return [value] if value.strip() else []
        return value

    @field_validator("possible_causes", mode="before")
    @classmethod
    def coerce_single_cause(cls, value: object) -> object:
        if isinstance(value, dict):
            return [value]
        return value

    class Config:
        populate_by_name = True


# ───────────── Task 4.2 — output cho Single-Agent baseline ─────────────


class LLMBaselineOutput(BaseModel):
    """Một LLM call trả về toàn bộ quyết định (nguyên nhân + dịch vụ + part + slot)."""

    out_of_scope: bool = Field(default=False, alias="outOfScope")
    possible_causes: List[LLMPossibleCause] = Field(
        default_factory=list, alias="possibleCauses"
    )
    service_ids: List[str] = Field(default_factory=list, alias="serviceIds")
    part_ids: List[str] = Field(default_factory=list, alias="partIds")
    slot_id: Optional[str] = Field(default=None, alias="slotId")
    missing_information: List[str] = Field(
        default_factory=list, alias="missingInformation"
    )
    explanation: str = ""

    @field_validator(
        "service_ids", "part_ids", "missing_information", mode="before"
    )
    @classmethod
    def coerce_string_to_list(cls, value: object) -> object:
        if value is None:
            return []
        if isinstance(value, str):
            return [value] if value.strip() else []
        return value

    @field_validator("possible_causes", mode="before")
    @classmethod
    def coerce_single_cause(cls, value: object) -> object:
        if isinstance(value, dict):
            return [value]
        return value

    class Config:
        populate_by_name = True

