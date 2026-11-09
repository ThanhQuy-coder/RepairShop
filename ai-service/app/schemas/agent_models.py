"""
Hợp đồng dữ liệu (wire contract) của lớp MAS — Task 2.1.

Nguyên tắc:
- camelCase ở JSON (alias), snake_case trong Python.
- `extra="forbid"` ở các object dữ liệu nghiệp vụ để chặn field lạ (ví dụ `costPrice`
  lọt từ Backend sang AI) => trả 422 thay vì âm thầm bỏ qua.
- Không có field nào cho phép AI "tạo" dữ liệu nghiệp vụ (không có `appointmentId`,
  `confirmed`, `priceOverride`...).

Tham chiếu: HUONG_DAN_TRIEN_KHAI_MAS_4_NGAY.md mục 5.1 / 5.2.
"""

from __future__ import annotations

import re
from datetime import datetime, timezone
from enum import Enum
from typing import Dict, List, Literal, Optional
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field, field_validator, model_validator

from app.schemas.request_models import DeviceInfo

_TIME_PATTERN = re.compile(r"^(?P<h>\d{2}):(?P<m>\d{2})(?::(?P<s>\d{2}))?$")
_DATE_PATTERN = re.compile(r"^\d{4}-\d{2}-\d{2}$")

DEFAULT_MAS_DISCLAIMER = (
    "Đây là thông tin tư vấn tham khảo. Kỹ thuật viên sẽ kiểm tra và xác nhận "
    "phương án sửa chữa thực tế."
)


class Intent(str, Enum):
    """5 nhãn intent của Coordinator."""

    DIAGNOSIS = "DIAGNOSIS"
    QUOTATION = "QUOTATION"
    APPOINTMENT = "APPOINTMENT"
    TRACKING = "TRACKING"
    UNKNOWN = "UNKNOWN"


class AgentName(str, Enum):
    COORDINATOR = "COORDINATOR"
    DIAGNOSIS = "DIAGNOSIS"
    QUOTATION = "QUOTATION"
    APPOINTMENT = "APPOINTMENT"
    # Dùng cho baseline so sánh (Task 4.2): một agent duy nhất xử lý toàn bộ yêu cầu.
    SINGLE_AGENT = "SINGLE_AGENT"


class AgentStatus(str, Enum):
    """Trạng thái tổng của response (mở rộng legacy: thêm PARTIAL/ERROR)."""

    SUCCESS = "SUCCESS"
    PARTIAL = "PARTIAL"
    NO_MATCH = "NO_MATCH"
    OUT_OF_SCOPE = "OUT_OF_SCOPE"
    ERROR = "ERROR"


class ActorType(str, Enum):
    GUEST = "GUEST"
    CUSTOMER = "CUSTOMER"


class Confidence(str, Enum):
    HIGH = "HIGH"
    MEDIUM = "MEDIUM"
    LOW = "LOW"


_VALID_CONFIDENCE = {level.value for level in Confidence}


class MasService(BaseModel):
    """Dịch vụ do Backend cung cấp (context) — AI chỉ được chọn trong danh sách này."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    service_id: str = Field(..., alias="serviceId", min_length=1, max_length=100)
    name: str = Field(..., min_length=1, max_length=150)
    description: Optional[str] = Field(default=None, max_length=1000)
    device_type: str = Field(..., alias="deviceType", min_length=1, max_length=50)
    base_price: float = Field(..., alias="basePrice", ge=0)

    @field_validator("device_type")
    @classmethod
    def device_type_lowercase(cls, value: str) -> str:
        normalized = value.strip().lower()
        if normalized not in {"phone", "laptop", "electronics"}:
            raise ValueError("deviceType phải là phone, laptop hoặc electronics.")
        return normalized


class MasPart(BaseModel):
    """Linh kiện do Backend cung cấp — KHÔNG có giá vốn (CostPrice)."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    part_id: str = Field(..., alias="partId", min_length=1, max_length=100)
    name: str = Field(..., min_length=1, max_length=150)
    unit_price: float = Field(..., alias="unitPrice", ge=0)
    compatible_device_type: Optional[str] = Field(
        default=None, alias="compatibleDeviceType", max_length=50
    )
    in_stock: bool = Field(default=True, alias="inStock")


class MasSlot(BaseModel):
    """Khung giờ còn trống do Backend cung cấp (chỉ đọc — AI không tạo slot)."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    time_slot_id: str = Field(..., alias="timeSlotId", min_length=1, max_length=100)
    slot_start: str = Field(..., alias="slotStart")
    slot_end: str = Field(..., alias="slotEnd")
    max_capacity: int = Field(default=0, alias="maxCapacity", ge=0)
    booked: int = Field(default=0, ge=0)
    available: int = Field(..., ge=0)
    is_full: Optional[bool] = Field(default=None, alias="isFull")
    appointment_date: Optional[str] = Field(default=None, alias="appointmentDate")

    @field_validator("slot_start", "slot_end")
    @classmethod
    def normalize_time(cls, value: str) -> str:
        match = _TIME_PATTERN.match(value.strip())
        if not match:
            raise ValueError("Giờ phải có định dạng HH:mm hoặc HH:mm:ss.")
        hour, minute = int(match.group("h")), int(match.group("m"))
        if hour > 23 or minute > 59:
            raise ValueError("Giờ không hợp lệ.")
        return f"{hour:02d}:{minute:02d}"

    @field_validator("appointment_date")
    @classmethod
    def validate_date(cls, value: Optional[str]) -> Optional[str]:
        if value is None:
            return None
        if not _DATE_PATTERN.match(value):
            raise ValueError("appointmentDate phải có định dạng YYYY-MM-DD.")
        return value

    @model_validator(mode="after")
    def check_consistency(self) -> "MasSlot":
        if self.slot_start >= self.slot_end:
            raise ValueError("slotStart phải nhỏ hơn slotEnd.")
        if self.max_capacity and self.booked > self.max_capacity:
            raise ValueError("booked không được lớn hơn maxCapacity.")
        computed_full = self.available == 0
        if self.is_full is None:
            self.is_full = computed_full
        elif self.is_full != computed_full:
            raise ValueError("isFull không khớp với available (isFull phải = available == 0).")
        return self


class MasContext(BaseModel):
    """Dữ liệu Backend đẩy sang — AI không tự truy vấn. Giới hạn kích thước để bảo vệ prompt."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    available_services: List[MasService] = Field(
        default_factory=list, alias="availableServices", max_length=200
    )
    available_parts: List[MasPart] = Field(
        default_factory=list, alias="availableParts", max_length=200
    )
    available_slots: List[MasSlot] = Field(
        default_factory=list, alias="availableSlots", max_length=60
    )


class AgentOptions(BaseModel):
    """Backend quyết định agent nào được phép chạy trong request này."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    allow_diagnosis: bool = Field(default=False, alias="allowDiagnosis")
    allow_quotation: bool = Field(default=False, alias="allowQuotation")
    allow_appointment: bool = Field(default=False, alias="allowAppointment")


class CustomerInfo(BaseModel):
    """Chỉ chứa thông tin người dùng tự nhập cho booking draft — không phải dữ liệu Backend."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    full_name: Optional[str] = Field(default=None, alias="fullName", max_length=100)
    phone: Optional[str] = Field(default=None, max_length=20)


class AgentRequest(BaseModel):
    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    request_id: UUID = Field(..., alias="requestId")
    actor_type: ActorType = Field(..., alias="actorType")
    message: str = Field(..., min_length=1, max_length=1000)
    device: DeviceInfo
    issue_description: str = Field(default="", alias="issueDescription", max_length=500)
    context: MasContext = Field(default_factory=MasContext)
    options: AgentOptions = Field(default_factory=AgentOptions)
    customer_info: CustomerInfo = Field(default_factory=CustomerInfo, alias="customerInfo")

    @field_validator("message")
    @classmethod
    def message_not_blank(cls, value: str) -> str:
        stripped = value.strip()
        if not stripped:
            raise ValueError("message không được để trống.")
        return stripped

    @field_validator("issue_description")
    @classmethod
    def issue_trimmed(cls, value: str) -> str:
        return value.strip()


# ─────────────────────────── Kết quả từng agent ────────────────────────────


class PossibleCause(BaseModel):
    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    name: str = Field(..., min_length=1, max_length=200)
    confidence: Confidence = Confidence.MEDIUM

    @field_validator("confidence", mode="before")
    @classmethod
    def normalize_confidence(cls, value: object) -> object:
        if isinstance(value, str):
            upper = value.strip().upper()
            return upper if upper in _VALID_CONFIDENCE else Confidence.MEDIUM.value
        return value


class SuggestedServiceOut(BaseModel):
    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    service_id: str = Field(..., alias="serviceId")
    service_name: str = Field(..., alias="serviceName", max_length=150)
    confidence: Confidence = Confidence.MEDIUM

    @field_validator("confidence", mode="before")
    @classmethod
    def normalize_confidence(cls, value: object) -> object:
        if isinstance(value, str):
            upper = value.strip().upper()
            return upper if upper in _VALID_CONFIDENCE else Confidence.MEDIUM.value
        return value


class SuggestedPartOut(BaseModel):
    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    part_id: str = Field(..., alias="partId")
    part_name: str = Field(..., alias="partName", max_length=150)
    confidence: Confidence = Confidence.MEDIUM


class DiagnosisResult(BaseModel):
    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    possible_causes: List[PossibleCause] = Field(
        default_factory=list, alias="possibleCauses"
    )
    suggested_services: List[SuggestedServiceOut] = Field(
        default_factory=list, alias="suggestedServices"
    )
    suggested_parts: List[SuggestedPartOut] = Field(
        default_factory=list, alias="suggestedParts"
    )
    missing_information: List[str] = Field(
        default_factory=list, alias="missingInformation"
    )
    explanation: str = Field(default="", max_length=600)


class QuotationItem(BaseModel):
    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    item_id: str = Field(..., alias="itemId")
    item_type: Literal["SERVICE", "PART"] = Field(..., alias="itemType")
    name: str = Field(..., min_length=1, max_length=150)
    quantity: int = Field(default=1, ge=1)
    unit_price: float = Field(..., alias="unitPrice", ge=0)
    line_total: float = Field(..., alias="lineTotal", ge=0)


class QuotationResult(BaseModel):
    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    items: List[QuotationItem] = Field(default_factory=list)
    estimated_total: float = Field(default=0, alias="estimatedTotal", ge=0)
    currency: str = "VND"
    assumptions: List[str] = Field(default_factory=list)
    explanation: str = Field(default="", max_length=600)
    disclaimer: str = DEFAULT_MAS_DISCLAIMER


class BookingDraft(BaseModel):
    """Bản nháp đặt lịch — KHÔNG phải lịch hẹn; Backend chỉ tạo khi người dùng xác nhận."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    full_name: Optional[str] = Field(default=None, alias="fullName", max_length=100)
    phone: Optional[str] = Field(default=None, max_length=20)
    appointment_date: Optional[str] = Field(default=None, alias="appointmentDate")
    time_slot_id: Optional[str] = Field(default=None, alias="timeSlotId")
    device_type: Optional[str] = Field(default=None, alias="deviceType", max_length=50)
    brand: Optional[str] = Field(default=None, max_length=100)
    model: Optional[str] = Field(default=None, max_length=100)
    issue_description: Optional[str] = Field(
        default=None, alias="issueDescription", max_length=500
    )


class SuggestedSlot(BaseModel):
    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    time_slot_id: str = Field(..., alias="timeSlotId")
    slot_start: str = Field(..., alias="slotStart")
    slot_end: str = Field(..., alias="slotEnd")
    available: int = Field(..., ge=0)
    is_full: bool = Field(..., alias="isFull")
    appointment_date: Optional[str] = Field(default=None, alias="appointmentDate")


class AppointmentResult(BaseModel):
    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    suggested_slots: List[SuggestedSlot] = Field(
        default_factory=list, alias="suggestedSlots"
    )
    booking_draft: Optional[BookingDraft] = Field(default=None, alias="bookingDraft")
    missing_information: List[str] = Field(
        default_factory=list, alias="missingInformation"
    )
    requires_user_confirmation: bool = Field(
        default=True, alias="requiresUserConfirmation"
    )
    explanation: str = Field(default="", max_length=600)


class AgentError(BaseModel):
    """Lỗi cục bộ của một agent — không ném ra ngoài graph."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    agent: str = Field(..., min_length=1, max_length=50)
    code: str = Field(..., min_length=1, max_length=50)
    message: str = Field(default="", max_length=300)


class AgentMeta(BaseModel):
    """Metadata phục vụ log/thực nghiệm — Backend KHÔNG trả xuống React."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    llm_mode: str = Field(default="ollama", alias="llmMode")
    llm_calls: int = Field(default=0, ge=0, alias="llmCalls")
    agent_timings_ms: Dict[str, int] = Field(
        default_factory=dict, alias="agentTimingsMs"
    )
    model: Optional[str] = None
    fallback_used: bool = Field(default=False, alias="fallbackUsed")


class AgentResponse(BaseModel):
    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    request_id: UUID = Field(..., alias="requestId")
    status: AgentStatus
    intent: Intent
    executed_agents: List[AgentName] = Field(
        default_factory=list, alias="executedAgents"
    )
    diagnosis: Optional[DiagnosisResult] = None
    quotation: Optional[QuotationResult] = None
    appointment: Optional[AppointmentResult] = None
    reason: str = Field(default="", max_length=500)
    disclaimer: Optional[str] = DEFAULT_MAS_DISCLAIMER
    processed_at: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="processedAt"
    )
    meta: AgentMeta = Field(default_factory=AgentMeta)

    @classmethod
    def error(cls, request_id: UUID, reason: str) -> "AgentResponse":
        return cls(
            requestId=request_id,
            status=AgentStatus.ERROR,
            intent=Intent.UNKNOWN,
            executedAgents=[],
            reason=reason[:500],
            disclaimer=None,
        )
