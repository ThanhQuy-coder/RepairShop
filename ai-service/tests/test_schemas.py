import uuid

import pytest
from pydantic import ValidationError

from app.schemas.agent_models import (
    AgentName,
    AgentRequest,
    AgentResponse,
    AgentStatus,
    Intent,
    MasContext,
    MasPart,
    SuggestedServiceOut,
    TrackingHistoryItemOut,
    TrackingResult,
    TrackingTicketOut,
    WarrantyLookupOut,
    WarrantyPackageOut,
    WarrantyResult,
)
from app.schemas.request_models import AdvisoryRequest
from app.schemas.response_models import AdvisoryResponse, AdvisoryStatus

VALID_PAYLOAD = {
    "requestId": "b3f1c2a0-1234-4a5b-9c1d-abcdef123456",
    "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"},
    "issueDescription": "Pin tụt nhanh",
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
            {"partId": "part-101", "name": "Pin iPhone 13", "unitPrice": 350000}
        ],
    },
}

VALID_AGENT_PAYLOAD = {
    "requestId": "b3f1c2a0-1234-4a5b-9c1d-abcdef123456",
    "actorType": "GUEST",
    "message": "Kiểm tra bảo hành và báo giá thay pin",
    "device": {"deviceType": "phone", "brand": "Apple", "model": "iPhone 13"},
    "issueDescription": "Pin tụt nhanh",
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
                "compatibleDeviceType": "phone",
                "inStock": True,
                "category": "Battery",
                "quantityOnHand": 5,
            }
        ],
        "availableSlots": [],
        "warrantyPolicies": [
            {
                "packageId": "wp-1",
                "name": "Bảo hành pin 6 tháng",
                "durationMonths": 6,
                "terms": "Bảo hành dung lượng pin.",
                "isActive": True,
            }
        ],
        "warrantyLookups": [],
        "trackingLookups": [],
        "diagnosisSymptoms": [],
    },
    "options": {
        "allowDiagnosis": True,
        "allowQuotation": True,
        "allowAppointment": False,
        "allowWarranty": True,
        "allowTracking": False,
    },
}


def test_valid_request_parses_successfully():
    req = AdvisoryRequest.model_validate(VALID_PAYLOAD)
    assert str(req.request_id) == VALID_PAYLOAD["requestId"]
    assert req.device.device_type.value == "phone"


@pytest.mark.parametrize(
    "device_type,expected",
    [
        ("phone", "phone"),
        ("laptop", "laptop"),
        ("electronics", "electronics"),
        ("tablet", "tablet"),
        ("smartwatch", "smartwatch"),
        ("Tablet", "tablet"),
        ("Smartwatch", "smartwatch"),
    ],
)
def test_all_five_device_types_parse_case_insensitively(device_type, expected):
    payload = {
        **VALID_PAYLOAD,
        "device": {**VALID_PAYLOAD["device"], "deviceType": device_type},
    }
    req = AdvisoryRequest.model_validate(payload)
    assert req.device.device_type.value == expected


def test_invalid_device_type_rejected():
    payload = {
        **VALID_PAYLOAD,
        "device": {**VALID_PAYLOAD["device"], "deviceType": "refrigerator"},
    }
    with pytest.raises(ValidationError):
        AdvisoryRequest.model_validate(payload)


def test_issue_description_over_500_chars_rejected():
    payload = {**VALID_PAYLOAD, "issueDescription": "a" * 501}
    with pytest.raises(ValidationError):
        AdvisoryRequest.model_validate(payload)


def test_blank_issue_description_rejected():
    payload = {**VALID_PAYLOAD, "issueDescription": "   "}
    with pytest.raises(ValidationError):
        AdvisoryRequest.model_validate(payload)


def test_negative_base_price_rejected():
    payload = {
        **VALID_PAYLOAD,
        "context": {
            "availableServices": [{**VALID_PAYLOAD["context"]["availableServices"][0], "basePrice": -100}],
            "availableParts": [],
        },
    }
    with pytest.raises(ValidationError):
        AdvisoryRequest.model_validate(payload)


def test_empty_context_lists_are_allowed():
    payload = {
        **VALID_PAYLOAD,
        "context": {"availableServices": [], "availableParts": []},
    }
    req = AdvisoryRequest.model_validate(payload)
    assert req.context.available_services == []


def test_extra_fields_forbidden_on_advisory_and_agent_requests():
    advisory_extra = {**VALID_PAYLOAD, "unexpectedField": "abc"}
    with pytest.raises(ValidationError):
        AdvisoryRequest.model_validate(advisory_extra)

    agent_extra = {**VALID_AGENT_PAYLOAD, "userId": "secret-user-id"}
    with pytest.raises(ValidationError):
        AgentRequest.model_validate(agent_extra)


@pytest.mark.parametrize(
    "forbidden_part_field",
    ["costPrice", "supplier", "minStockLevel"],
)
def test_internal_part_fields_rejected_by_strict_contract(forbidden_part_field):
    part_payload = {
        "partId": "part-1",
        "name": "Pin iPhone 13",
        "unitPrice": 350000,
        forbidden_part_field: 100000,
    }
    with pytest.raises(ValidationError):
        MasPart.model_validate(part_payload)


def test_agent_request_rejects_missing_required_wrong_type_and_invalid_enum():
    missing_req = dict(VALID_AGENT_PAYLOAD)
    missing_req.pop("requestId")
    with pytest.raises(ValidationError):
        AgentRequest.model_validate(missing_req)

    wrong_type = {**VALID_AGENT_PAYLOAD, "requestId": "not-a-uuid"}
    with pytest.raises(ValidationError):
        AgentRequest.model_validate(wrong_type)

    invalid_actor = {**VALID_AGENT_PAYLOAD, "actorType": "ADMIN"}
    with pytest.raises(ValidationError):
        AgentRequest.model_validate(invalid_actor)


def test_normalized_intents_agents_and_statuses_complete():
    assert {item.value for item in Intent} == {
        "DIAGNOSIS",
        "QUOTATION",
        "APPOINTMENT",
        "WARRANTY",
        "TRACKING",
        "UNKNOWN",
    }
    assert {item.value for item in AgentStatus} == {
        "SUCCESS",
        "PARTIAL",
        "NEED_CLARIFICATION",
        "NO_MATCH",
        "OUT_OF_SCOPE",
        "ERROR",
    }
    assert {item.value for item in AdvisoryStatus} == {item.value for item in AgentStatus}
    assert {AgentName.WARRANTY.value, AgentName.TRACKING.value}.issubset(
        {item.value for item in AgentName}
    )


def test_agent_request_and_response_warranty_tracking_roundtrip():
    req = AgentRequest.model_validate(
        {
            **VALID_AGENT_PAYLOAD,
            "ticketCode": "RT-20261114-0001",
            "warrantyCode": "WR-20261114-0001",
        }
    )
    assert req.ticket_code == "RT-20261114-0001"
    assert req.warranty_code == "WR-20261114-0001"
    assert req.options.allow_warranty is True

    svc = SuggestedServiceOut(
        serviceId="svc-001", serviceName="Thay pin", confidence="HIGH", matchingScore=0.92
    )
    assert svc.matching_score == pytest.approx(0.92)

    resp = AgentResponse(
        requestId=uuid.UUID(VALID_AGENT_PAYLOAD["requestId"]),
        status=AgentStatus.NEED_CLARIFICATION,
        intent=Intent.WARRANTY,
        executedAgents=[AgentName.WARRANTY],
        warranty=WarrantyResult(
            policies=[
                WarrantyPackageOut(
                    packageId="wp-1",
                    name="Bảo hành 6 tháng",
                    durationMonths=6,
                    terms="Lỗi kỹ thuật được hỗ trợ",
                )
            ],
            warranties=[
                WarrantyLookupOut(
                    warrantyCode="WR-20261114-0001",
                    ticketCode="RT-20261114-0001",
                    deviceLabel="Apple iPhone 13",
                    status="Active",
                    startDate="2026-10-01",
                    endDate="2027-04-01",
                    isExpired=False,
                )
            ],
            missingInformation=[],
            requiresVerification=False,
            explanation="Còn hạn bảo hành.",
        ),
        tracking=TrackingResult(
            tickets=[
                TrackingTicketOut(
                    ticketCode="RT-20261114-0001",
                    deviceLabel="Apple iPhone 13",
                    status="InRepair",
                    statusLabel="Đang sửa chữa",
                    statusHistory=[
                        TrackingHistoryItemOut(
                            status="CheckedIn",
                            statusLabel="Đã tiếp nhận",
                            changedAt="2026-11-14T08:00:00Z",
                        )
                    ],
                )
            ],
            missingInformation=[],
            explanation="Đang sửa chữa.",
        ),
        missingInformation=["Cần xác nhận khung giờ mong muốn"],
        reason="Đã kiểm tra thông tin bảo hành.",
    )
    dumped = resp.model_dump(by_alias=True, mode="json")
    assert dumped["status"] == "NEED_CLARIFICATION"
    assert dumped["warranty"]["warranties"][0]["warrantyCode"] == "WR-20261114-0001"
    assert dumped["tracking"]["tickets"][0]["ticketCode"] == "RT-20261114-0001"
