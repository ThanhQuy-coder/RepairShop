from app.prompts.advisory_prompt import build_user_prompt
from app.prompts.system_prompt import SYSTEM_PROMPT
from app.retrieval.retrieval_service import RetrievalResult
from app.schemas.request_models import (
    AvailablePart,
    AvailableService,
    DeviceInfo,
    DeviceType,
)


def test_system_prompt_declares_scope_boundaries():
    assert "must NOT" in SYSTEM_PROMPT
    assert "outOfScope" in SYSTEM_PROMPT
    assert "Never invent" in SYSTEM_PROMPT


def test_user_prompt_only_includes_candidate_data_passed_in():
    device = DeviceInfo(deviceType=DeviceType.PHONE, brand="iPhone", model="13")
    retrieval = RetrievalResult(
        candidate_services=[
            AvailableService(
                serviceId="svc-1", name="Thay pin", deviceType="phone", basePrice=400000
            )
        ],
        candidate_parts=[
            AvailablePart(partId="part-1", name="Pin iPhone 13", unitPrice=350000)
        ],
    )

    prompt = build_user_prompt(device, "pin tụt nhanh", retrieval)

    assert "svc-1" in prompt
    assert "part-1" in prompt
    assert "iPhone" in prompt
