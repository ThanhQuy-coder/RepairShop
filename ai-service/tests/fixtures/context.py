"""Dữ liệu context mẫu cho test MAS — KHÔNG kết nối DB, phản ánh đúng shape Backend gửi sang.

Quy ước ID (dùng lại trong eval_cases.json):
- Dịch vụ: svc-pin, svc-man-hinh, svc-cam-ung, svc-ve-sinh, svc-sac, svc-loa, svc-camera,
  svc-ban-phim, svc-pin-laptop, svc-tan-nhiet
- Linh kiện: part-pin-ip13, part-man-hinh-ip13 (hết hàng), part-pin-dell
- Khung giờ: slot-0900 (trống), slot-1400 (đầy), slot-1500 (còn 1)
"""

from __future__ import annotations

from typing import Any, Dict, List, Optional

REQUEST_ID = "b24f61c0-3f75-4c1e-b19e-4d5d2c7f22e1"

CORE_SERVICES: List[Dict[str, Any]] = [
    {
        "serviceId": "svc-pin",
        "name": "Thay pin",
        "description": "Thay pin tương thích cho điện thoại",
        "deviceType": "phone",
        "basePrice": 400000,
    },
    {
        "serviceId": "svc-man-hinh",
        "name": "Thay màn hình",
        "deviceType": "phone",
        "basePrice": 900000,
    },
    {
        "serviceId": "svc-cam-ung",
        "name": "Thay cảm ứng",
        "deviceType": "phone",
        "basePrice": 700000,
    },
    {
        "serviceId": "svc-ve-sinh",
        "name": "Vệ sinh main",
        "deviceType": "phone",
        "basePrice": 150000,
    },
    {
        "serviceId": "svc-sac",
        "name": "Sửa chân sạc",
        "deviceType": "phone",
        "basePrice": 250000,
    },
    {
        "serviceId": "svc-loa",
        "name": "Thay loa",
        "deviceType": "phone",
        "basePrice": 200000,
    },
    {
        "serviceId": "svc-camera",
        "name": "Thay camera",
        "deviceType": "phone",
        "basePrice": 600000,
    },
    {
        "serviceId": "svc-ban-phim",
        "name": "Thay bàn phím laptop",
        "deviceType": "laptop",
        "basePrice": 800000,
    },
    {
        "serviceId": "svc-pin-laptop",
        "name": "Thay pin laptop",
        "deviceType": "laptop",
        "basePrice": 650000,
    },
    {
        "serviceId": "svc-tan-nhiet",
        "name": "Vệ sinh tản nhiệt laptop",
        "deviceType": "laptop",
        "basePrice": 250000,
    },
]

CORE_PARTS: List[Dict[str, Any]] = [
    {
        "partId": "part-pin-ip13",
        "name": "Pin iPhone 13",
        "unitPrice": 350000,
        "compatibleDeviceType": "phone",
        "inStock": True,
    },
    {
        "partId": "part-man-hinh-ip13",
        "name": "Màn hình iPhone 13",
        "unitPrice": 1200000,
        "compatibleDeviceType": "phone",
        "inStock": False,
    },
    {
        "partId": "part-pin-dell",
        "name": "Pin Dell Inspiron 15",
        "unitPrice": 650000,
        "compatibleDeviceType": "laptop",
        "inStock": True,
    },
]

CORE_SLOTS: List[Dict[str, Any]] = [
    {
        "timeSlotId": "slot-0900",
        "slotStart": "09:00",
        "slotEnd": "10:00",
        "maxCapacity": 2,
        "booked": 0,
        "available": 2,
        "isFull": False,
        "appointmentDate": "2026-11-10",
    },
    {
        "timeSlotId": "slot-1400",
        "slotStart": "14:00",
        "slotEnd": "15:00",
        "maxCapacity": 2,
        "booked": 2,
        "available": 0,
        "isFull": True,
        "appointmentDate": "2026-11-10",
    },
    {
        "timeSlotId": "slot-1500",
        "slotStart": "15:00",
        "slotEnd": "16:00",
        "maxCapacity": 2,
        "booked": 1,
        "available": 1,
        "isFull": False,
        "appointmentDate": "2026-11-10",
    },
]


def phone_device() -> Dict[str, str]:
    return {"deviceType": "phone", "brand": "iPhone", "model": "13"}


def laptop_device() -> Dict[str, str]:
    return {"deviceType": "laptop", "brand": "Dell", "model": "Inspiron 15"}


def build_agent_payload(
    message: str,
    *,
    device: Optional[Dict[str, str]] = None,
    issue_description: str = "",
    services: Optional[List[Dict[str, Any]]] = None,
    parts: Optional[List[Dict[str, Any]]] = None,
    slots: Optional[List[Dict[str, Any]]] = None,
    allow_diagnosis: bool = True,
    allow_quotation: bool = True,
    allow_appointment: bool = True,
    actor_type: str = "GUEST",
    customer_info: Optional[Dict[str, str]] = None,
) -> Dict[str, Any]:
    return {
        "requestId": REQUEST_ID,
        "actorType": actor_type,
        "message": message,
        "device": device or phone_device(),
        "issueDescription": issue_description,
        "context": {
            "availableServices": CORE_SERVICES if services is None else services,
            "availableParts": CORE_PARTS if parts is None else parts,
            "availableSlots": CORE_SLOTS if slots is None else slots,
        },
        "options": {
            "allowDiagnosis": allow_diagnosis,
            "allowQuotation": allow_quotation,
            "allowAppointment": allow_appointment,
        },
        "customerInfo": customer_info or {"fullName": None, "phone": None},
    }


def build_advisory_payload(
    issue_description: str,
    *,
    device: Optional[Dict[str, str]] = None,
    services: Optional[List[Dict[str, Any]]] = None,
    parts: Optional[List[Dict[str, Any]]] = None,
) -> Dict[str, Any]:
    """Payload cho endpoint legacy /v1/advisory (không có slots/options)."""
    return {
        "requestId": REQUEST_ID,
        "device": device or phone_device(),
        "issueDescription": issue_description,
        "context": {
            "availableServices": services or CORE_SERVICES[:1],
            "availableParts": parts or CORE_PARTS[:1],
        },
    }
