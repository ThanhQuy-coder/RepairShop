"""
State dùng chung cho LangGraph (Task 2.1 — Day 2).

`AgentState` chỉ tồn tại trong phạm vi MỘT request, KHÔNG lưu lịch sử hội thoại
(xem MAS_ARCHITECTURE.md mục 4.3 và HUONG_DAN_TRIEN_KHAI_MAS_4_NGAY.md mục 4.4).

Enum được khai báo DUY NHẤT ở `agent_models.py` để tránh trùng lặp/không đồng bộ.
"""

from __future__ import annotations

from typing import Any, Dict, List, Optional, TypedDict

from app.schemas.agent_models import (
    AgentError,
    AgentName,
    AgentResponse,
    AgentStatus,
    AppointmentResult,
    DiagnosisResult,
    Intent,
    QuotationResult,
)

__all__ = [
    "AgentError",
    "AgentName",
    "AgentResponse",
    "AgentState",
    "AgentStatus",
    "AppointmentResult",
    "DiagnosisResult",
    "Intent",
    "QuotationResult",
]


class AgentState(TypedDict, total=False):
    """
    State dùng chung giữa các node. `total=False` vì mỗi node chỉ trả về phần field
    của mình (LangGraph merge partial state vào state hiện có).
    """

    # ── Input (từ Backend, không đổi trong suốt request) ──
    request_id: str
    user_message: str
    actor_type: str
    device: Dict[str, Any]
    issue_description: str
    available_services: List[Dict[str, Any]]
    available_parts: List[Dict[str, Any]]
    available_slots: List[Dict[str, Any]]
    options: Dict[str, bool]
    customer_info: Dict[str, Optional[str]]
    reference_date: Optional[str]

    # ── Coordinator ──
    intent: str
    intent_confidence: float
    plan: List[str]

    # ── Kết quả agent ──
    executed_agents: List[str]
    diagnosis_result: Optional[DiagnosisResult]
    quotation_result: Optional[QuotationResult]
    appointment_result: Optional[AppointmentResult]

    # ── Vận hành ──
    errors: List[AgentError]
    timings: Dict[str, int]
    llm_calls: int
    fallback_used: bool
    out_of_scope: bool
    final_status: str
    final_reason: str
    final_response: Optional[AgentResponse]
