from __future__ import annotations

import json

from app.retrieval.retrieval_service import RetrievalResult
from app.schemas.request_models import DeviceInfo


def build_user_prompt(device: DeviceInfo, issue_description: str, retrieval: RetrievalResult) -> str:
    """
    Ghép User Issue + Retrieved Context thành 1 user prompt duy nhất (đúng sơ đồ Task 6.7:
    User Issue + Retrieved Context -> LLM). Candidate list đưa vào dạng JSON có cấu trúc rõ ràng
    để LLM dễ trích xuất đúng serviceId/partId, giảm khả năng "bịa" ID.
    """
    candidate_services = [
        {"serviceId": s.service_id, "name": s.name, "basePrice": s.base_price}
        for s in retrieval.candidate_services
    ]
    candidate_parts = [
        {"partId": p.part_id, "name": p.name, "unitPrice": p.unit_price}
        for p in retrieval.candidate_parts
    ]

    payload = {
        "device": {
            "deviceType": device.device_type.value,
            "brand": device.brand,
            "model": device.model,
        },
        "issueDescription": issue_description,
        "candidateServices": candidate_services,
        "candidateParts": candidate_parts,
    }

    return (
        "Analyze the following device issue and candidate data, then respond with the required JSON "
        "shape described in the system prompt.\n\n"
        f"{json.dumps(payload, ensure_ascii=False, indent=2)}"
    )