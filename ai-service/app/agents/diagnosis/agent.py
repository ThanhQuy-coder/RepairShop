"""Diagnosis Agent (TASK-08).

Pipeline: chuẩn hoá mô tả -> trích xuất thực thể (brand, model, triệu chứng chuẩn)
-> retrieval (Service Matching + 3-Layer Part Compatibility) -> LLM (nếu bật) hoặc Rule fallback
-> gắn matchingScore và xác định trạng thái NEED_CLARIFICATION / NO_MATCH khi cần.

Ràng buộc:
- Chỉ chọn ID nằm trong `candidateServices` / `candidateParts` (loại ID lạ).
- Chỉ nêu NGUYÊN NHÂN KHẢ DĨ, không kết luận chắc chắn.
- Không sinh giá, không tạo lịch hẹn, không ghi dữ liệu.
"""

from __future__ import annotations

import logging
import time
from typing import Dict, List, Optional

from app.agents.diagnosis.symptom_rules import build_explanation, infer_causes
from app.core.config import settings
from app.core.text import extract_brand_and_model, normalize_colloquial_vietnamese
from app.llm.llm_client import LLMClient, LLMResponseError, LLMTimeoutError, llm_client
from app.prompts.diagnosis_prompt import (
    DIAGNOSIS_SYSTEM_PROMPT,
    build_diagnosis_user_prompt,
)
from app.retrieval.retrieval_service import RetrievalResult, retrieve_candidates
from app.retrieval.symptom_lexicon import map_text_to_canonical_symptoms
from app.schemas.agent_models import (
    AgentError,
    AgentName,
    Confidence,
    DiagnosisResult,
    IdentifiedEntities,
    PossibleCause,
    SuggestedPartOut,
    SuggestedServiceOut,
)
from app.schemas.llm_output_models import LLMDiagnosisOutput
from app.schemas.request_models import (
    AdvisoryContext,
    AvailablePart,
    AvailableService,
    DeviceInfo,
)
from app.schemas.state_models import AgentState

logger = logging.getLogger(__name__)

MAX_SERVICES = 3
MAX_PARTS = 2


def _build_context(state: AgentState) -> AdvisoryContext:
    services = [
        AvailableService.model_validate(item)
        for item in (state.get("available_services") or [])
    ]
    parts = [
        AvailablePart.model_validate(item) for item in (state.get("available_parts") or [])
    ]
    return AdvisoryContext(availableServices=services, availableParts=parts)


def _service_confidence(causes: List[PossibleCause]) -> Confidence:
    if any(cause.confidence is Confidence.HIGH for cause in causes):
        return Confidence.HIGH
    return Confidence.MEDIUM


def _rule_based_result(
    issue_description: str, retrieval: RetrievalResult
) -> DiagnosisResult:
    causes = infer_causes(issue_description)
    confidence = _service_confidence(causes)

    matched_services = [
        s for s in retrieval.candidate_services if retrieval.service_scores.get(s.service_id, 0) > 0
    ]
    matched_parts = [
        p for p in retrieval.candidate_parts if retrieval.part_scores.get(p.part_id, 0) > 0
    ]

    services = [
        SuggestedServiceOut(
            serviceId=service.service_id,
            serviceName=service.name,
            confidence=confidence if index == 0 else Confidence.MEDIUM,
            matchingScore=retrieval.service_normalized_scores.get(service.service_id),
        )
        for index, service in enumerate(matched_services[:MAX_SERVICES])
    ]
    parts = [
        SuggestedPartOut(
            partId=part.part_id,
            partName=part.name,
            confidence=Confidence.MEDIUM,
        )
        for part in matched_parts[:MAX_PARTS]
    ]

    return DiagnosisResult(
        possibleCauses=causes,
        suggestedServices=services,
        suggestedParts=parts,
        missingInformation=[],
        explanation=build_explanation(issue_description, causes),
    )


def _map_llm_output(
    output: LLMDiagnosisOutput,
    retrieval: RetrievalResult,
    context: AdvisoryContext,
    device: DeviceInfo,
) -> DiagnosisResult:
    service_by_id = {
        service.service_id: service
        for service in context.available_services
        if service.device_type.lower() in (device.device_type.value, "general")
    }
    part_by_id = {
        part.part_id: part
        for part in context.available_parts
        if (part.compatible_device_type or "").lower() in ("", "general", device.device_type.value)
        and part.in_stock
        and (part.quantity_on_hand is None or part.quantity_on_hand > 0)
    }

    causes = [
        PossibleCause(
            name=cause.name,
            confidence=Confidence(cause.confidence.strip().upper())
            if cause.confidence.strip().upper() in {c.value for c in Confidence}
            else Confidence.MEDIUM,
        )
        for cause in output.possible_causes[:3]
    ]
    confidence = _service_confidence(causes)

    services: List[SuggestedServiceOut] = []
    seen_service_ids: set[str] = set()
    for service_id in output.suggested_service_ids:
        if len(services) >= MAX_SERVICES:
            break
        if service_id in seen_service_ids:
            continue
        seen_service_ids.add(service_id)
        candidate = service_by_id.get(service_id)
        if candidate is None:
            logger.warning("Diagnosis LLM trả serviceId lạ '%s' — đã loại bỏ.", service_id)
            continue
        services.append(
            SuggestedServiceOut(
                serviceId=candidate.service_id,
                serviceName=candidate.name,
                confidence=confidence,
                matchingScore=retrieval.service_normalized_scores.get(candidate.service_id),
            )
        )

    parts: List[SuggestedPartOut] = []
    seen_part_ids: set[str] = set()
    for part_id in output.suggested_part_ids:
        if len(parts) >= MAX_PARTS:
            break
        if part_id in seen_part_ids:
            continue
        seen_part_ids.add(part_id)
        candidate = part_by_id.get(part_id)
        if candidate is None:
            logger.warning("Diagnosis LLM trả partId lạ '%s' — đã loại bỏ.", part_id)
            continue
        parts.append(
            SuggestedPartOut(
                partId=candidate.part_id,
                partName=candidate.name,
                confidence=Confidence.MEDIUM,
            )
        )

    missing = [item[:120] for item in output.missing_information[:5]]
    explanation = (output.explanation or "").strip()[:300]
    if not explanation:
        explanation = build_explanation("", causes)

    return DiagnosisResult(
        possibleCauses=causes,
        suggestedServices=services,
        suggestedParts=parts,
        missingInformation=missing,
        explanation=explanation,
    )


class DiagnosisAgent:
    """Cho phép inject LLM client để test — production dùng singleton `llm_client`."""

    def __init__(self, client: Optional[LLMClient] = None) -> None:
        self._client = client or llm_client

    async def run(self, state: AgentState) -> Dict[str, object]:
        started = time.perf_counter()

        device = DeviceInfo.model_validate(state["device"])
        raw_issue = (state.get("issue_description") or state.get("user_message") or "").strip()
        issue_description = normalize_colloquial_vietnamese(raw_issue)
        context = _build_context(state)
        retrieval = retrieve_candidates(device, issue_description, context)

        brand, model = extract_brand_and_model(
            raw_issue, hint_brand=device.brand, hint_model=device.model
        )
        canonical_symptoms = map_text_to_canonical_symptoms(raw_issue, device.device_type.value)
        identified = IdentifiedEntities(
            deviceType=device.device_type.value,
            brand=brand,
            model=model,
            symptoms=canonical_symptoms,
        )

        errors: List[AgentError] = list(state.get("errors") or [])
        llm_calls = int(state.get("llm_calls") or 0)
        fallback_used = bool(state.get("fallback_used"))
        out_of_scope = False
        need_clarification = bool(state.get("need_clarification"))
        missing_info: List[str] = list(state.get("missing_information") or [])

        if not context.available_services and not context.available_parts:
            missing_item = "mô tả tình trạng cụ thể hơn hoặc danh mục dịch vụ"
            missing_info.append(missing_item)
            result = DiagnosisResult(
                possibleCauses=[],
                suggestedServices=[],
                suggestedParts=[],
                missingInformation=[missing_item],
                explanation=(
                    "Chưa có dữ liệu dịch vụ phù hợp để đối chiếu. "
                    "Vui lòng mô tả rõ hơn hoặc mang máy đến kiểm tra trực tiếp."
                ),
            )
        elif retrieval.is_vague_issue and not retrieval.matched_any_service:
            need_clarification = True
            missing_item = "triệu chứng cụ thể của thiết bị (ví dụ: tụt pin, sọc màn hình, không sạc được)"
            missing_info.append(missing_item)
            result = DiagnosisResult(
                possibleCauses=[],
                suggestedServices=[],
                suggestedParts=[],
                missingInformation=[missing_item],
                explanation=(
                    "Mô tả hiện tại còn chung chung nên chưa thể khoanh vùng lỗi. "
                    "Bạn vui lòng cho biết dấu hiệu cụ thể để nhận gợi ý sát hơn."
                ),
            )
        elif (settings.llm_mode or "ollama").lower() == "offline":
            result = _rule_based_result(issue_description, retrieval)
            fallback_used = True
            if not result.suggested_services and not result.suggested_parts:
                result = result.model_copy(
                    update={
                        "explanation": (
                            "Chưa tìm thấy dịch vụ phù hợp với triệu chứng mô tả trong danh mục hiện có. "
                            "Vui lòng mang máy tới cửa hàng để kỹ thuật viên kiểm tra trực tiếp."
                        )
                    }
                )
        else:
            prompt = build_diagnosis_user_prompt(device, issue_description, retrieval)
            try:
                raw_output = await self._client.generate_json(
                    DIAGNOSIS_SYSTEM_PROMPT, prompt
                )
                llm_calls += 1
                output = LLMDiagnosisOutput.model_validate(raw_output)
            except LLMTimeoutError:
                raise
            except (LLMResponseError, ValueError) as exc:
                logger.warning("Diagnosis LLM lỗi (%s) — dùng fallback rule.", exc)
                errors.append(
                    AgentError(
                        agent=AgentName.DIAGNOSIS.value,
                        code="LLM_RESPONSE_INVALID",
                        message="Phản hồi LLM không hợp lệ, đã dùng phương án dự phòng.",
                    )
                )
                result = _rule_based_result(issue_description, retrieval)
                fallback_used = True
            else:
                if output.out_of_scope:
                    out_of_scope = True
                    result = DiagnosisResult(
                        possibleCauses=[],
                        suggestedServices=[],
                        suggestedParts=[],
                        missingInformation=[],
                        explanation="Yêu cầu nằm ngoài phạm vi tư vấn sửa chữa thiết bị.",
                    )
                else:
                    result = _map_llm_output(output, retrieval, context, device)
                    if not result.suggested_services and not result.suggested_parts:
                        rule_result = _rule_based_result(issue_description, retrieval)
                        result = DiagnosisResult(
                            possibleCauses=result.possible_causes or rule_result.possible_causes,
                            suggestedServices=rule_result.suggested_services,
                            suggestedParts=rule_result.suggested_parts,
                            missingInformation=result.missing_information,
                            explanation=result.explanation,
                        )
                        fallback_used = True

        elapsed_ms = int((time.perf_counter() - started) * 1000)
        timings = dict(state.get("timings") or {})
        timings[AgentName.DIAGNOSIS.value] = elapsed_ms
        executed = list(state.get("executed_agents") or [])
        if AgentName.DIAGNOSIS.value not in executed:
            executed.append(AgentName.DIAGNOSIS.value)

        logger.info(
            "diagnosis request_id=%s services=%d parts=%d causes=%d fallback=%s elapsed_ms=%d",
            state.get("request_id"),
            len(result.suggested_services),
            len(result.suggested_parts),
            len(result.possible_causes),
            fallback_used,
            elapsed_ms,
        )

        return {
            "diagnosis_result": result,
            "identified_entities": identified,
            "need_clarification": need_clarification,
            "missing_information": list(dict.fromkeys(missing_info)),
            "errors": errors,
            "executed_agents": executed,
            "timings": timings,
            "llm_calls": llm_calls,
            "fallback_used": fallback_used,
            "out_of_scope": out_of_scope,
        }


_diagnosis_agent = DiagnosisAgent()


async def diagnosis_node(state: AgentState) -> Dict[str, object]:
    return await _diagnosis_agent.run(state)
