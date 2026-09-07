from __future__ import annotations

from enum import Enum
from typing import List
from uuid import UUID

from pydantic import BaseModel, Field, field_validator # type: ignore


class DeviceType(str, Enum):
    """Đúng Field Dictionary Request — Task 5 Tuần 2: enum(phone, laptop, electronics)."""
    PHONE = "phone"
    LAPTOP = "laptop"
    ELECTRONICS = "electronics"


class DeviceInfo(BaseModel):
    device_type: DeviceType = Field(..., alias="deviceType")
    brand: str = Field(..., min_length=1, max_length=100)
    model: str = Field(..., min_length=1, max_length=100)

    @field_validator("brand", "model")
    @classmethod
    def not_blank(cls, value: str) -> str:
        if not value.strip():
            raise ValueError("Không được để trống hoặc chỉ chứa khoảng trắng.")
        return value.strip()

    class Config:
        populate_by_name = True


class AvailableService(BaseModel):
    """Khớp context.availableServices[] trong contract — dữ liệu THẬT do Backend gửi kèm."""
    service_id: str = Field(..., alias="serviceId", min_length=1)
    name: str = Field(..., min_length=1, max_length=150)
    device_type: str = Field(..., alias="deviceType")
    base_price: float = Field(..., alias="basePrice", ge=0)

    class Config:
        populate_by_name = True


class AvailablePart(BaseModel):
    """Khớp context.availableParts[] trong contract."""
    part_id: str = Field(..., alias="partId", min_length=1)
    name: str = Field(..., min_length=1, max_length=150)
    unit_price: float = Field(..., alias="unitPrice", ge=0)

    class Config:
        populate_by_name = True


class AdvisoryContext(BaseModel):
    """
    'context.availableServices' và 'context.availableParts' BẮT BUỘC gửi kèm mỗi lần gọi
    (AI Service stateless, không tự lưu dữ liệu giá — đúng nguyên tắc đã chốt Task 5 Tuần 2).
    Cho phép danh sách rỗng (VD: cửa hàng chưa có dịch vụ nào cho loại thiết bị này) —
    khi đó AI sẽ trả về NO_MATCH ở bước xử lý, không phải lỗi request.
    """
    available_services: List[AvailableService] = Field(default_factory=list, alias="availableServices")
    available_parts: List[AvailablePart] = Field(default_factory=list, alias="availableParts")

    class Config:
        populate_by_name = True


class AdvisoryRequest(BaseModel):
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

    class Config:
        populate_by_name = True
        json_schema_extra = {
            "example": {
                "requestId": "b3f1c2a0-1234-4a5b-9c1d-abcdef123456",
                "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"},
                "issueDescription": "Pin tụt nhanh, máy nóng khi sạc, thỉnh thoảng tự tắt nguồn",
                "context": {
                    "availableServices": [
                        {"serviceId": "svc-001", "name": "Thay pin", "deviceType": "phone", "basePrice": 400000}
                    ],
                    "availableParts": [
                        {"partId": "part-101", "name": "Pin iPhone 13", "unitPrice": 350000}
                    ],
                },
            }
        }