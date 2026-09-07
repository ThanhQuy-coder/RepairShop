from __future__ import annotations

import re
from dataclasses import dataclass, field
from typing import List

from app.retrieval.symptom_lexicon import expand_issue_keywords
from app.schemas.request_models import (
    AdvisoryContext,
    AvailablePart,
    AvailableService,
    DeviceInfo,
)


@dataclass
class RetrievalResult:
    """Candidate đã lọc — KHÔNG phải kết quả cuối, chỉ là input cho LLM (Task 6.7)."""

    candidate_services: List[AvailableService] = field(default_factory=list)
    candidate_parts: List[AvailablePart] = field(default_factory=list)


MAX_CANDIDATES = 8


def _tokenize(text: str) -> set[str]:
    return set(re.findall(r"[a-zA-ZÀ-ỹ0-9]+", text.lower()))


def _score(expanded_tokens: set[str], name: str) -> int:
    return len(expanded_tokens & _tokenize(name))


def retrieve_candidates(
    device: DeviceInfo, issue_description: str, context: AdvisoryContext
) -> RetrievalResult:
    """
    Checklist Task 6.6:
    1) Filter theo deviceType — loại ngay dịch vụ không cùng loại thiết bị.
    2) Match keyword/semantic issue — mở rộng token qua SYMPTOM_KEYWORD_MAP rồi mới chấm điểm,
       để "pin tụt nhanh" liên kết được với dịch vụ tên "Thay pin".
    3) Retrieve services / 4) Retrieve parts — 2 danh sách candidate riêng biệt.
    5) Giới hạn số candidate — cắt còn MAX_CANDIDATES mỗi loại.
    6) Không tự tạo service/part không tồn tại — hàm này CHỈ SẮP XẾP LẠI danh sách context đã có,
       không bao giờ tạo object mới ngoài những gì Backend đã gửi lên.
    """
    issue_tokens = _tokenize(issue_description)
    expanded_tokens = expand_issue_keywords(issue_tokens)

    # 1) Filter theo deviceType
    same_type_services = [
        s
        for s in context.available_services
        if s.device_type.lower() == device.device_type.value
    ]
    all_parts = list(
        context.available_parts
    )  # Part không gắn deviceType trong contract (Task 5 Tuần 2)

    # 2) Match keyword/semantic + 3/4) Retrieve services/parts
    ranked_services = sorted(
        same_type_services, key=lambda s: _score(expanded_tokens, s.name), reverse=True
    )
    ranked_parts = sorted(
        all_parts, key=lambda p: _score(expanded_tokens, p.name), reverse=True
    )

    # Loại bỏ candidate hoàn toàn không liên quan (score = 0) khi danh sách đủ dài để lọc bớt —
    # nhưng giữ lại ít nhất kết quả nếu không có candidate nào ghi điểm (tránh mất hết dữ liệu
    # trong trường hợp cửa hàng chỉ có vài dịch vụ, để LLM vẫn có gì đó tham khảo).
    scored_services = [
        s for s in ranked_services if _score(expanded_tokens, s.name) > 0
    ] or ranked_services
    scored_parts = [
        p for p in ranked_parts if _score(expanded_tokens, p.name) > 0
    ] or ranked_parts

    # 5) Giới hạn số candidate
    return RetrievalResult(
        candidate_services=scored_services[:MAX_CANDIDATES],
        candidate_parts=scored_parts[:MAX_CANDIDATES],
    )
