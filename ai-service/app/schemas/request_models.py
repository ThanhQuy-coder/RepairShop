from __future__ import annotations

from enum import Enum
from typing import List, Optional
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field, field_validator


_VALID_CONTEXT_DEVICE_TYPES = {
    "phone",
    "laptop",
    "electronics",
    "tablet",
    "smartwatch",
    "general",
}


class DeviceType(str, Enum):
    """Đồng bộ với RepairShop.Domain.Common.Enums.DeviceType: phone, laptop, electronics, tablet, smartwatch."""

    PHONE = "phone"
    LAPTOP = "laptop"
    ELECTRONICS = "electronics"
    TABLET = "tablet"
    SMARTWATCH = "smartwatch"


class DeviceInfo(BaseModel):
    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    device_type: DeviceType = Field(..., alias="deviceType")
    brand: str = Field(..., min_length=1, max_length=100)
    model: str = Field(..., min_length=1, max_length=100)

    @field_validator("device_type", mode="before")
    @classmethod
    def normalize_device_type(cls, value: object) -> object:
        if isinstance(value, str):
            stripped = value.strip().lower()
            if not stripped:
                raise ValueError("deviceType không được để trống.")
            return stripped
        return value

    @field_validator("brand", "model")
    @classmethod
    def not_blank(cls, value: str) -> str:
        if not value.strip():
            raise ValueError("Không được để trống hoặc chỉ chứa khoảng trắng.")
        return value.strip()


class AvailableService(BaseModel):
    """Khớp context.availableServices[] trong contract — dữ liệu THẬT do Backend gửi kèm."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    service_id: str = Field(..., alias="serviceId", min_length=1)
    name: str = Field(..., min_length=1, max_length=150)
    description: Optional[str] = Field(default=None, max_length=1000)
    device_type: str = Field(..., alias="deviceType", min_length=1, max_length=50)
    base_price: float = Field(..., alias="basePrice", ge=0)

    @field_validator("device_type", mode="before")
    @classmethod
    def validate_service_device_type(cls, value: object) -> str:
        if not isinstance(value, str) or not value.strip():
            raise ValueError("deviceType không được để trống.")
        normalized = value.strip().lower()
        if normalized not in _VALID_CONTEXT_DEVICE_TYPES:
            raise ValueError(
                "deviceType phải thuộc phone, laptop, electronics, tablet, smartwatch hoặc general."
            )
        return normalized


class AvailablePart(BaseModel):
    """Khớp context.availableParts[] trong contract."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    part_id: str = Field(..., alias="partId", min_length=1)
    name: str = Field(..., min_length=1, max_length=150)
    unit_price: float = Field(..., alias="unitPrice", ge=0)
    compatible_device_type: Optional[str] = Field(
        default=None, alias="compatibleDeviceType", max_length=50
    )
    in_stock: bool = Field(default=True, alias="inStock")
    category: Optional[str] = Field(default=None, max_length=100)
    quantity_on_hand: Optional[int] = Field(
        default=None, alias="quantityOnHand", ge=0
    )

    @field_validator("compatible_device_type", mode="before")
    @classmethod
    def validate_compatible_device_type(cls, value: object) -> Optional[str]:
        if value is None:
            return None
        if isinstance(value, str):
            normalized = value.strip().lower()
            if not normalized:
                return None
            if normalized not in _VALID_CONTEXT_DEVICE_TYPES:
                raise ValueError(
                    "compatibleDeviceType phải thuộc phone, laptop, electronics, tablet, smartwatch hoặc general."
                )
            return normalized
        raise ValueError("compatibleDeviceType phải là chuỗi hợp lệ.")


class AdvisoryContext(BaseModel):
    """
    'context.availableServices' và 'context.availableParts' BẮT BUỘC gửi kèm mỗi lần gọi
    (AI Service stateless, không tự lưu dữ liệu giá — đúng nguyên tắc đã chốt Task 5 Tuần 2).
    """

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    available_services: List[AvailableService] = Field(
        default_factory=list, alias="availableServices"
    )
    available_parts: List[AvailablePart] = Field(
        default_factory=list, alias="availableParts"
    )


class AdvisoryRequest(BaseModel):
    model_config = ConfigDict(
        populate_by_name=True,
        extra="forbid",
        json_schema_extra={
            "example": {
                "requestId": "b3f1c2a0-1234-4a5b-9c1d-abcdef123456",
                "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"},
                "issueDescription": "Pin tụt nhanh, máy nóng khi sạc, thỉnh thoảng tự tắt nguồn",
                "context": {
                    "availableServices": [
                        {
                            "serviceId": "svc-001",
                            "name": "Thay pin",
                            "deviceType": "phone",
                            "basePrice": 400000,
                        }
                    ],
                    "availableParts": [
                        {
                            "partId": "part-101",
                            "name": "Pin iPhone 13",
                            "unitPrice": 350000,
                        }
                    ],
                },
            }
        },
    )

    request_id: UUID = Field(..., alias="requestId")
    device: DeviceInfo
    issue_description: str = Field(..., alias="issueDescription", min_length=1, max_length=500)
    context: AdvisoryContext

    @field_validator("issue_description")
    @classmethod
    def issue_description_not_blank(cls, value: str) -> str:
        stripped = value.strip()
        if not stripped:
            raise ValueError("issueDescription không được để trống hoặc chỉ chứa khoảng trắng.")
        return stripped