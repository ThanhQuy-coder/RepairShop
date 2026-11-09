"""
Single-Agent baseline (Task 4.2) — đối chứng với MAS.

Một LLM call duy nhất nhận toàn bộ context và trả về: nguyên nhân khả dĩ, serviceIds,
partIds, slotId. Giá được tính lại từ context (giống MAS) để phép so sánh công bằng:
khác biệt duy nhất là **một agent/one-shot prompt** thay vì pipeline nhiều agent.

Dùng cùng classifier intent với MAS (AD-06) để cô lập khác biệt ở bước xử lý nghiệp vụ.
"""

from __future__ import annotations

import logging
import time
from datetime import date
from typing import Callable, Dict, List, Optional

from app.agents.appointment.agent import AppointmentAgent
from app.agents.diagnosis.agent import _rule_based_result
from app.agents.quotation.agent import QuotationAgent
from app.core.config import settings
from app.core.text import normalize_text
from app.llm.llm_client import LLMClient, LLMResponseError, LLMTimeoutError, llm_client
from app.ml.intent_classifier import classify_intent
from app.orchestration.graph import _initial_state
from app.orchestration.postprocess import validate_node
from app.prompts.baseline_prompt import (
    BASELINE_SYSTEM_PROMPT,
    build_baseline_user_prompt,
)
from app.retrieval.retrieval_service import retrieve_candidates
from app.schemas.agent_models import (
    AgentError,
    AgentName,
    AgentRequest,
    AgentResponse,
    AppointmentResult,
    BookingDraft,
    Confidence,
    DiagnosisResult,
    Intent,
    MasPart,
    MasService,
    MasSlot,
    PossibleCause,
    SuggestedPartOut,
    SuggestedServiceOut,
    SuggestedSlot,
)
from app.schemas.llm_output_models import LLMBaselineOutput
from app.schemas.request_models import (
    AdvisoryContext,
    AvailablePart,
    AvailableService,
    DeviceInfo,
)
from app.schemas.state_models import AgentState

logger = logging.getLogger(__name__)


def _services(state: AgentState) -> Dict[str, MasService]:
    return {
        item.service_id: item
        for item in (MasService.model_validate(raw) for raw in state.get("available_services") or [])
    }


def _parts(state: AgentState) -> Dict[str, MasPart]:
    return {
        item.part_id: item
        for item in (MasPart.model_validate(raw) for raw in state.get("available_parts") or [])
    }


def _slots(state: AgentState) -> Dict[str, MasSlot]:
    return {
        item.time_slot_id: item
        for item in (MasSlot.model_validate(raw) for raw in state.get("available_slots") or [])
    }


class SingleAgentBaseline:
    def __init__(
        self,
        client: Optional[LLMClient] = None,
        today_provider: Optional[Callable[[], date]] = None,
    ) -> None:
        self._client = client or llm_client
        self._today = today_provider or date.today

    async def run(self, request: AgentRequest) -> AgentResponse:
        started = time.perf_counter()
        state = _initial_state(request)

        text = f"{request.message} {request.issue_description}".strip()
        signals = classify_intent(text)
        state["intent"] = signals.intent.value
        state["errors"] = []
        state["llm_calls"] = 0
        state["fallback_used"] = False

        if signals.intent in (Intent.UNKNOWN, Intent.TRACKING):
            state["plan"] = []
            state["timings"] = {"SINGLE_AGENT": int((time.perf_counter() - started) * 1000)}
            return (await validate_node(state))["final_response"]

        state["plan"] = [AgentName.SINGLE_AGENT.value]

        if (settings.llm_mode or "ollama").lower() == "offline":
            await self._offline(state, request)
            state["fallback_used"] = True
        else:
            try:
                raw_output = await self._client.generate_json(
                    BASELINE_SYSTEM_PROMPT, build_baseline_user_prompt(request)
                )
                state["llm_calls"] = 1
                output = LLMBaselineOutput.model_validate(raw_output)
            except LLMTimeoutError:
                raise
            except (LLMResponseError, ValueError) as exc:
                logger.warning("Baseline LLM lỗi (%s) — dùng fallback rule.", exc)
                state["errors"] = [
                    AgentError(
                        agent=AgentName.SINGLE_AGENT.value,
                        code="LLM_RESPONSE_INVALID",
                        message="Phản hồi LLM không hợp lệ, đã dùng phương án dự phòng.",
                    )
                ]
                await self._offline(state, request)
                state["fallback_used"] = True
            else:
                if output.out_of_scope:
                    state["out_of_scope"] = True
                else:
                    await self._apply_llm_output(state, request, output)

        state["executed_agents"] = (
            [] if state.get("out_of_scope") else [AgentName.SINGLE_AGENT.value]
        )
        timings = dict(state.get("timings") or {})
        timings[AgentName.SINGLE_AGENT.value] = int((time.perf_counter() - started) * 1000)
        state["timings"] = timings

        return (await validate_node(state))["final_response"]

    async def _apply_llm_output(
        self, state: AgentState, request: AgentRequest, output: LLMBaselineOutput
    ) -> None:
        services = _services(state)
        parts = _parts(state)

        causes = [
            PossibleCause(
                name=cause.name,
                confidence=Confidence(cause.confidence.strip().upper())
                if cause.confidence.strip().upper() in {level.value for level in Confidence}
                else Confidence.MEDIUM,
            )
            for cause in output.possible_causes[:3]
        ]

        service_ids = list(dict.fromkeys(output.service_ids))[:3]
        part_ids = list(dict.fromkeys(output.part_ids))[:2]

        suggested_services = [
            SuggestedServiceOut(
                serviceId=services[service_id].service_id,
                serviceName=services[service_id].name,
                confidence=Confidence.HIGH if i == 0 else Confidence.MEDIUM,
            )
            for i, service_id in enumerate(service_ids)
            if service_id in services
        ]
        suggested_parts = [
            SuggestedPartOut(
                partId=parts[part_id].part_id,
                partName=parts[part_id].name,
                confidence=Confidence.MEDIUM,
            )
            for part_id in part_ids
            if part_id in parts
        ]

        state["diagnosis_result"] = DiagnosisResult(
            possibleCauses=causes,
            suggestedServices=suggested_services,
            suggestedParts=suggested_parts,
            missingInformation=[item[:120] for item in output.missing_information[:5]],
            explanation=(output.explanation or "")[:600],
        )

        if request.options.allow_quotation:
            quotation = await QuotationAgent().run(state)
            state["quotation_result"] = quotation.get("quotation_result")

        if request.options.allow_appointment:
            state["appointment_result"] = self._appointment_from_slot(state, output.slot_id)

    def _appointment_from_slot(
        self, state: AgentState, slot_id: Optional[str]
    ) -> AppointmentResult:
        slots = _slots(state)
        chosen = slots.get(slot_id) if slot_id else None
        customer = state.get("customer_info") or {}
        device = state.get("device") or {}

        def draft(target: Optional[MasSlot]) -> BookingDraft:
            raw_phone = "".join(
                character for character in (customer.get("phone") or "") if character.isdigit()
            )
            phone = raw_phone if 9 <= len(raw_phone) <= 11 else None
            return BookingDraft(
                fullName=(customer.get("fullName") or "").strip() or None,
                phone=phone,
                appointmentDate=target.appointment_date if target else None,
                timeSlotId=target.time_slot_id if target else None,
                deviceType=device.get("deviceType"),
                brand=device.get("brand"),
                model=device.get("model"),
                issueDescription=state.get("issue_description") or None,
            )

        def missing_fields(booking: BookingDraft) -> List[str]:
            missing: List[str] = []
            if not booking.full_name:
                missing.append("fullName")
            if not booking.phone:
                missing.append("phone")
            return missing

        if chosen is not None and chosen.available > 0 and not chosen.is_full:
            booking = draft(chosen)
            return AppointmentResult(
                suggestedSlots=[
                    SuggestedSlot(
                        timeSlotId=chosen.time_slot_id,
                        slotStart=chosen.slot_start,
                        slotEnd=chosen.slot_end,
                        available=chosen.available,
                        isFull=bool(chosen.is_full),
                        appointmentDate=chosen.appointment_date,
                    )
                ],
                bookingDraft=booking,
                missingInformation=missing_fields(booking),
                requiresUserConfirmation=True,
                explanation="Khung giờ do trợ lý chọn từ danh sách còn trống của cửa hàng.",
            )

        available = [slot for slot in slots.values() if slot.available > 0]
        booking = draft(None)
        return AppointmentResult(
            suggestedSlots=[
                SuggestedSlot(
                    timeSlotId=slot.time_slot_id,
                    slotStart=slot.slot_start,
                    slotEnd=slot.slot_end,
                    available=slot.available,
                    isFull=bool(slot.is_full),
                    appointmentDate=slot.appointment_date,
                )
                for slot in available[:3]
            ],
            bookingDraft=booking,
            missingInformation=missing_fields(booking) + ["timeSlotId"],
            requiresUserConfirmation=True,
            explanation="Vui lòng chọn một khung giờ còn trống.",
        )

    async def _offline(self, state: AgentState, request: AgentRequest) -> None:
        """Đường deterministic (chỉ dùng cho test/offline, không dùng trong thực nghiệm)."""
        device = DeviceInfo.model_validate(state["device"])
        issue = normalize_text(state.get("issue_description") or state.get("user_message") or "")
        context = AdvisoryContext(
            availableServices=[
                AvailableService.model_validate(raw)
                for raw in state.get("available_services") or []
            ],
            availableParts=[
                AvailablePart.model_validate(raw) for raw in state.get("available_parts") or []
            ],
        )
        retrieval = retrieve_candidates(device, issue, context)

        if not retrieval.candidate_services and not retrieval.candidate_parts:
            return

        state["diagnosis_result"] = _rule_based_result(issue, retrieval)

        if request.options.allow_quotation:
            quotation = await QuotationAgent().run(state)
            state["quotation_result"] = quotation.get("quotation_result")

        if request.options.allow_appointment:
            appointment = await AppointmentAgent(today_provider=self._today).run(state)
            state["appointment_result"] = appointment.get("appointment_result")


_single_agent_baseline = SingleAgentBaseline()


async def run_baseline(request: AgentRequest) -> AgentResponse:
    return await _single_agent_baseline.run(request)
