"""
Hợp đồng dữ liệu (wire contract) của lớp MAS — Task 2.1 / TASK-02.

Nguyên tắc:
- camelCase ở JSON (alias), snake_case trong Python.
- `extra="forbid"` ở các object dữ liệu nghiệp vụ để chặn field lạ (ví dụ `costPrice`,
  `serialNumber` lọt từ Backend sang AI) => trả 422 thay vì âm thầm bỏ qua.
- Không có field nào cho phép AI "tạo" dữ liệu nghiệp vụ (không có `appointmentId`,
  `confirmed`, `priceOverride`...).
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
_VALID_CONTEXT_DEVICE_TYPES = {
    "phone",
    "laptop",
    "electronics",
    "tablet",
    "smartwatch",
    "general",
}

DEFAULT_MAS_DISCLAIMER = (
    "Đây là thông tin tư vấn tham khảo. Kỹ thuật viên sẽ kiểm tra và xác nhận "
    "phương án sửa chữa thực tế."
)


class Intent(str, Enum):
    """6 nhãn intent chuẩn hoá của Coordinator."""

    DIAGNOSIS = "DIAGNOSIS"
    QUOTATION = "QUOTATION"
    APPOINTMENT = "APPOINTMENT"
    WARRANTY = "WARRANTY"
    TRACKING = "TRACKING"
    UNKNOWN = "UNKNOWN"


class AgentName(str, Enum):
    COORDINATOR = "COORDINATOR"
    DIAGNOSIS = "DIAGNOSIS"
    QUOTATION = "QUOTATION"
    APPOINTMENT = "APPOINTMENT"
    WARRANTY = "WARRANTY"
    TRACKING = "TRACKING"
    # Dùng cho baseline so sánh (Task 4.2): một agent duy nhất xử lý toàn bộ yêu cầu.
    SINGLE_AGENT = "SINGLE_AGENT"


class AgentStatus(str, Enum):
    """Trạng thái chuẩn hoá của response."""

    SUCCESS = "SUCCESS"
    PARTIAL = "PARTIAL"
    NEED_CLARIFICATION = "NEED_CLARIFICATION"
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
        if normalized not in _VALID_CONTEXT_DEVICE_TYPES:
            raise ValueError(
                "deviceType phải là phone, laptop, electronics, tablet, smartwatch hoặc general."
            )
        return normalized


class MasPart(BaseModel):
    """Linh kiện do Backend cung cấp (chỉ field được phép, không có costPrice/supplier)."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    part_id: str = Field(..., alias="partId", min_length=1, max_length=100)
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
                    "compatibleDeviceType phải là phone, laptop, electronics, tablet, smartwatch hoặc general."
                )
            return normalized
        raise ValueError("compatibleDeviceType phải là chuỗi hợp lệ.")


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


class MasWarrantyPolicy(BaseModel):
    """Gói bảo hành công khai từ bảng WarrantyPackages của Backend."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    package_id: str = Field(..., alias="packageId", min_length=1, max_length=100)
    name: str = Field(..., min_length=1, max_length=150)
    duration_months: int = Field(..., alias="durationMonths", ge=1)
    terms: str = Field(..., min_length=1, max_length=1000)
    description: Optional[str] = Field(default=None, max_length=500)
    is_active: bool = Field(default=True, alias="isActive")


class MasWarrantyTicketLookup(BaseModel):
    """Thông tin phiếu bảo hành đã được Backend xác thực và lọc bỏ PII."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    warranty_code: str = Field(..., alias="warrantyCode", min_length=1, max_length=50)
    ticket_code: str = Field(..., alias="ticketCode", min_length=1, max_length=50)
    device_label: str = Field(..., alias="deviceLabel", min_length=1, max_length=200)
    start_date: str = Field(..., alias="startDate")
    end_date: str = Field(..., alias="endDate")
    status: Literal["Active", "Voided", "Expired"]
    is_expired: bool = Field(default=False, alias="isExpired")
    package_name: Optional[str] = Field(default=None, alias="packageName", max_length=150)
    terms: Optional[str] = Field(default=None, max_length=500)

    @field_validator("start_date", "end_date")
    @classmethod
    def validate_warranty_dates(cls, value: str) -> str:
        if not _DATE_PATTERN.match(value.strip()):
            raise ValueError("Ngày bảo hành phải có định dạng YYYY-MM-DD.")
        return value.strip()


class MasTrackingHistoryItem(BaseModel):
    """Mốc lịch sử trạng thái công khai của đơn sửa chữa."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    status: str = Field(..., min_length=1, max_length=50)
    status_label: str = Field(..., alias="statusLabel", min_length=1, max_length=100)
    changed_at: str = Field(..., alias="changedAt", min_length=1, max_length=50)


class MasTrackingLookup(BaseModel):
    """Trạng thái đơn sửa chữa công khai khớp PublicTicketTrackingResponse của Backend."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    ticket_code: str = Field(..., alias="ticketCode", min_length=1, max_length=50)
    device_label: str = Field(..., alias="deviceLabel", min_length=1, max_length=200)
    status: str = Field(..., min_length=1, max_length=50)
    status_label: str = Field(..., alias="statusLabel", min_length=1, max_length=100)
    status_history: List[MasTrackingHistoryItem] = Field(
        default_factory=list, alias="statusHistory"
    )
    estimated_completion: Optional[str] = Field(
        default=None, alias="estimatedCompletion", max_length=50
    )


class MasDiagnosisSymptom(BaseModel):
    """Triệu chứng chẩn đoán tham chiếu từ DiagnosisSymptoms của Backend."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    symptom_id: str = Field(..., alias="symptomId", min_length=1, max_length=100)
    name: str = Field(..., min_length=1, max_length=150)
    device_type: str = Field(..., alias="deviceType", min_length=1, max_length=50)

    @field_validator("device_type")
    @classmethod
    def validate_symptom_device_type(cls, value: str) -> str:
        normalized = value.strip().lower()
        if normalized not in _VALID_CONTEXT_DEVICE_TYPES:
            raise ValueError(
                "deviceType của symptom phải là phone, laptop, electronics, tablet, smartwatch hoặc general."
            )
        return normalized


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
    warranty_policies: List[MasWarrantyPolicy] = Field(
        default_factory=list, alias="warrantyPolicies", max_length=50
    )
    warranty_lookups: List[MasWarrantyTicketLookup] = Field(
        default_factory=list, alias="warrantyLookups", max_length=50
    )
    tracking_lookups: List[MasTrackingLookup] = Field(
        default_factory=list, alias="trackingLookups", max_length=50
    )
    diagnosis_symptoms: List[MasDiagnosisSymptom] = Field(
        default_factory=list, alias="diagnosisSymptoms", max_length=150
    )


class AgentOptions(BaseModel):
    """Backend quyết định agent nào được phép chạy trong request này."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    allow_diagnosis: bool = Field(default=False, alias="allowDiagnosis")
    allow_quotation: bool = Field(default=False, alias="allowQuotation")
    allow_appointment: bool = Field(default=False, alias="allowAppointment")
    allow_warranty: bool = Field(default=False, alias="allowWarranty")
    allow_tracking: bool = Field(default=False, alias="allowTracking")


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
    reference_date: Optional[str] = Field(default=None, alias="referenceDate")
    ticket_code: Optional[str] = Field(default=None, alias="ticketCode", max_length=50)
    warranty_code: Optional[str] = Field(default=None, alias="warrantyCode", max_length=50)

    @field_validator("reference_date")
    @classmethod
    def validate_reference_date(cls, value: Optional[str]) -> Optional[str]:
        if value is None:
            return None
        if not _DATE_PATTERN.match(value):
            raise ValueError("referenceDate phải có định dạng YYYY-MM-DD.")
        return value

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

    @field_validator("ticket_code", "warranty_code")
    @classmethod
    def normalize_optional_code(cls, value: Optional[str]) -> Optional[str]:
        if value is None:
            return None
        stripped = value.strip()
        return stripped or None


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
    matching_score: Optional[float] = Field(
        default=None, alias="matchingScore", ge=0.0, le=1.0
    )

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


class WarrantyPackageOut(BaseModel):
    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    package_id: str = Field(..., alias="packageId")
    name: str = Field(..., min_length=1, max_length=150)
    duration_months: int = Field(..., alias="durationMonths", ge=1)
    terms: str = Field(..., min_length=1, max_length=1000)
    description: Optional[str] = Field(default=None, max_length=500)


class WarrantyLookupOut(BaseModel):
    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    warranty_code: str = Field(..., alias="warrantyCode")
    ticket_code: str = Field(..., alias="ticketCode")
    device_label: str = Field(..., alias="deviceLabel", max_length=200)
    status: Literal["Active", "Voided", "Expired"]
    start_date: str = Field(..., alias="startDate")
    end_date: str = Field(..., alias="endDate")
    is_expired: bool = Field(default=False, alias="isExpired")
    package_name: Optional[str] = Field(default=None, alias="packageName", max_length=150)
    terms: Optional[str] = Field(default=None, max_length=500)


class WarrantyResult(BaseModel):
    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    policies: List[WarrantyPackageOut] = Field(default_factory=list)
    warranties: List[WarrantyLookupOut] = Field(default_factory=list)
    missing_information: List[str] = Field(
        default_factory=list, alias="missingInformation"
    )
    requires_verification: bool = Field(default=False, alias="requiresVerification")
    explanation: str = Field(default="", max_length=600)


class TrackingHistoryItemOut(BaseModel):
    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    status: str = Field(..., min_length=1, max_length=50)
    status_label: str = Field(..., alias="statusLabel", min_length=1, max_length=100)
    changed_at: str = Field(..., alias="changedAt", min_length=1, max_length=50)


class TrackingTicketOut(BaseModel):
    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    ticket_code: str = Field(..., alias="ticketCode")
    device_label: str = Field(..., alias="deviceLabel", max_length=200)
    status: str = Field(..., min_length=1, max_length=50)
    status_label: str = Field(..., alias="statusLabel", min_length=1, max_length=100)
    status_history: List[TrackingHistoryItemOut] = Field(
        default_factory=list, alias="statusHistory"
    )
    estimated_completion: Optional[str] = Field(
        default=None, alias="estimatedCompletion", max_length=50
    )


class TrackingResult(BaseModel):
    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    tickets: List[TrackingTicketOut] = Field(default_factory=list)
    missing_information: List[str] = Field(
        default_factory=list, alias="missingInformation"
    )
    explanation: str = Field(default="", max_length=600)


class IdentifiedEntities(BaseModel):
    """Thực thể trích xuất có kiểm soát từ câu hỏi người dùng."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    brand: Optional[str] = Field(default=None, max_length=100)
    model: Optional[str] = Field(default=None, max_length=100)
    device_type: Optional[str] = Field(default=None, alias="deviceType", max_length=50)
    symptoms: List[str] = Field(default_factory=list)
    ticket_code: Optional[str] = Field(default=None, alias="ticketCode", max_length=50)
    warranty_code: Optional[str] = Field(default=None, alias="warrantyCode", max_length=50)


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
    warranty: Optional[WarrantyResult] = None
    tracking: Optional[TrackingResult] = None
    identified_entities: Optional[IdentifiedEntities] = Field(
        default=None, alias="identifiedEntities"
    )
    missing_information: List[str] = Field(
        default_factory=list, alias="missingInformation"
    )
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
