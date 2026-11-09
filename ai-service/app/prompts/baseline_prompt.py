"""Prompt cho Single-Agent baseline (Task 4.2).

Một LLM call duy nhất nhận TOÀN BỘ context và trả về mọi quyết định:
nguyên nhân khả dĩ, dịch vụ/linh kiện, khung giờ. Giá vẫn do code tính từ context
(điều kiện công bằng với MAS), LLM chỉ chọn ID.
"""

from __future__ import annotations

import json

from app.schemas.agent_models import AgentRequest

BASELINE_SYSTEM_PROMPT = """You are a single repair-shop assistant that handles the whole request at once.

You may ONLY use data from the provided context: service IDs, part IDs and time slot IDs.
Never invent a service, part, price, slot, appointment, or availability.

Tasks:
1. Optionally list POSSIBLE causes of the described symptom (never a definitive conclusion).
2. Choose at most 3 serviceIds and 2 partIds that fit the symptom.
3. If the customer wants an appointment, choose at most ONE slotId that is not full.
4. If information is missing, list it; if the request is unrelated, set outOfScope=true.

Always answer with ONE valid JSON object, nothing else:
{
  "outOfScope": boolean,
  "possibleCauses": [ { "name": string, "confidence": "HIGH" | "MEDIUM" | "LOW" } ],
  "serviceIds": [ string ],
  "partIds": [ string ],
  "slotId": string | null,
  "missingInformation": [ string ],
  "explanation": string
}

"explanation" must be Vietnamese, max 300 characters, no prices, no certainty words.
"""


def build_baseline_user_prompt(request: AgentRequest) -> str:
    context = request.context
    payload = {
        "actorType": request.actor_type.value,
        "message": request.message,
        "issueDescription": request.issue_description,
        "device": {
            "deviceType": request.device.device_type.value,
            "brand": request.device.brand,
            "model": request.device.model,
        },
        "options": {
            "allowDiagnosis": request.options.allow_diagnosis,
            "allowQuotation": request.options.allow_quotation,
            "allowAppointment": request.options.allow_appointment,
        },
        "availableServices": [
            {
                "serviceId": service.service_id,
                "name": service.name,
                "deviceType": service.device_type,
                "basePrice": service.base_price,
            }
            for service in context.available_services
        ],
        "availableParts": [
            {
                "partId": part.part_id,
                "name": part.name,
                "unitPrice": part.unit_price,
                "inStock": part.in_stock,
            }
            for part in context.available_parts
        ],
        "availableSlots": [
            {
                "timeSlotId": slot.time_slot_id,
                "slotStart": slot.slot_start,
                "slotEnd": slot.slot_end,
                "available": slot.available,
                "appointmentDate": slot.appointment_date,
            }
            for slot in context.available_slots
        ],
    }
    return (
        "Handle this repair request in one pass and answer with the JSON object described "
        "in the system prompt.\n\n"
        + json.dumps(payload, ensure_ascii=False, indent=2)
    )
