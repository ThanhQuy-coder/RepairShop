"""
Diagnosis Agent (Task 2.2).

Pipeline: chuẩn hoá mô tả -> retrieval (context Backend) -> LLM (nếu bật) -> map ID
trong candidate -> DiagnosisResult. Khi LLM tắt/lỗi: dùng luật triệu chứng + retrieval.

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
from app.core.text import normalize_text
from app.llm.llm_client import LLMClient, LLMResponseError, LLMTimeoutError, llm_client
from app.prompts.diagnosis_prompt import (
    DIAGNOSIS_SYSTEM_PROMPT,
    build_diagnosis_user_prompt,
)
from app.retrieval.retrieval_service import RetrievalResult, retrieve_candidates
from app.schemas.agent_models import (
    AgentError,
    AgentName,
    Confidence,
    DiagnosisResult,
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

    services = [
        SuggestedServiceOut(
            serviceId=service.service_id,
            serviceName=service.name,
            confidence=confidence if index == 0 else Confidence.MEDIUM,
        )
        for index, service in enumerate(retrieval.candidate_services[:MAX_SERVICES])
    ]
    parts = [
        SuggestedPartOut(
            partId=part.part_id,
            partName=part.name,
            confidence=Confidence.MEDIUM,
        )
        for part in retrieval.candidate_parts[:MAX_PARTS]
    ]

    return DiagnosisResult(
        possibleCauses=causes,
        suggestedServices=services,
        suggestedParts=parts,
        missingInformation=[],
        explanation=build_explanation(issue_description, causes),
    )


def _map_llm_output(
    output: LLMDiagnosisOutput, retrieval: RetrievalResult
) -> DiagnosisResult:
    service_by_id = {service.service_id: service for service in retrieval.candidate_services}
    part_by_id = {part.part_id: part for part in retrieval.candidate_parts}

    causes = [
        PossibleCause(name=cause.name, confidence=Confidence(cause.confidence.strip().upper())
                      if cause.confidence.strip().upper() in {c.value for c in Confidence}
                      else Confidence.MEDIUM)
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
                serviceName=candidate.name,  # tên lấy từ dữ liệu thật
                confidence=confidence,
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
                partId=candidate.part_id, partName=candidate.name, confidence=Confidence.MEDIUM
            )
        )

    missing = [item[:120] for item in output.missing_information[:5]]
    explanation = (output.explanation or "").strip()[:600]
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
        issue_description = normalize_text(
            state.get("issue_description") or state.get("user_message") or ""
        )
        retrieval = retrieve_candidates(device, issue_description, _build_context(state))

        errors: List[AgentError] = list(state.get("errors") or [])
        llm_calls = int(state.get("llm_calls") or 0)
        fallback_used = bool(state.get("fallback_used"))
        out_of_scope = False

        if not retrieval.candidate_services and not retrieval.candidate_parts:
            result = DiagnosisResult(
                possibleCauses=[],
                suggestedServices=[],
                suggestedParts=[],
                missingInformation=["mô tả tình trạng cụ thể hơn hoặc danh mục dịch vụ"],
                explanation=(
                    "Chưa đủ dữ liệu để đối chiếu dịch vụ phù hợp. "
                    "Vui lòng mô tả rõ hơn hoặc mang máy đến kiểm tra trực tiếp."
                ),
            )
        elif (settings.llm_mode or "ollama").lower() == "offline":
            result = _rule_based_result(issue_description, retrieval)
            fallback_used = True
        else:
            prompt = build_diagnosis_user_prompt(device, issue_description, retrieval)
            try:
                raw_output = await self._client.generate_json(
                    DIAGNOSIS_SYSTEM_PROMPT, prompt
                )
                llm_calls += 1
                output = LLMDiagnosisOutput.model_validate(raw_output)
            except LLMTimeoutError:
                # Timeout phải nổi lên endpoint để trả 408 AI_TIMEOUT (Backend fallback).
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
                    result = _map_llm_output(output, retrieval)
                    if not result.suggested_services and not result.suggested_parts:
                        # LLM không chọn được ID hợp lệ nào: dùng luật trên context thật
                        # thay vì trả rỗng (không bịa dữ liệu, chỉ xếp hạng candidate).
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
