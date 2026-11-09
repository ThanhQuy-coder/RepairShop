from __future__ import annotations

import re
from dataclasses import dataclass, field
from typing import List

from app.retrieval.symptom_lexicon import expand_text_keywords
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
    # Task 3.1: điểm số để Quotation chỉ lấy nhóm liên quan nhất (tránh báo giá lan man).
    service_scores: dict[str, int] = field(default_factory=dict)
    part_scores: dict[str, int] = field(default_factory=dict)


MAX_CANDIDATES = 8

# Trọng số cộng thêm khi tên linh kiện/dịch vụ chứa brand hoặc model của thiết bị.
_BRAND_MODEL_BONUS = 2


def _tokenize(text: str) -> set[str]:
    return set(re.findall(r"[a-zA-ZÀ-ỹ0-9]+", text.lower()))


def _score(terms: set[str], name: str, brand: str = "", model: str = "") -> int:
    """
    Chấm điểm tên candidate theo 2 thành phần:
    1) Số token trùng với tập từ khóa đã mở rộng (hành vi cũ).
    2) Điểm ưu tiên brand/model — CHỈ cộng khi tên candidate đã khớp ít nhất 1 từ khóa,
       để "Pin iPhone 13" thắng "Pin Samsung" mà không kéo "Màn hình iPhone 13" vào kết quả
       chỉ vì cùng brand/model (sửa D-07).
    """
    name_tokens = _tokenize(name)
    overlap = len(terms & name_tokens)
    if overlap == 0:
        return 0

    score = overlap
    for value in (brand, model):
        for token in _tokenize(value):
            if len(token) >= 2 and token in name_tokens:
                score += _BRAND_MODEL_BONUS
    return score


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
    expanded_tokens = issue_tokens | expand_text_keywords(issue_description)

    # 1) Filter theo deviceType
    same_type_services = [
        s
        for s in context.available_services
        if s.device_type.lower() == device.device_type.value
    ]
    # Part chỉ được xét nếu tương thích deviceType (nếu Backend có gửi) và còn hàng (Task 3.1).
    all_parts = [
        part
        for part in context.available_parts
        if (part.compatible_device_type or "").lower()
        in ("", device.device_type.value)
        and part.in_stock
    ]

    # 2) Match keyword/semantic + 3/4) Retrieve services/parts
    ranked_services = sorted(
        same_type_services,
        key=lambda s: _score(expanded_tokens, s.name, device.brand, device.model),
        reverse=True,
    )
    ranked_parts = sorted(
        all_parts,
        key=lambda p: _score(expanded_tokens, p.name, device.brand, device.model),
        reverse=True,
    )

    # Loại bỏ candidate hoàn toàn không liên quan (score = 0) khi danh sách đủ dài để lọc bớt —
    # nhưng giữ lại ít nhất kết quả nếu không có candidate nào ghi điểm (tránh mất hết dữ liệu
    # trong trường hợp cửa hàng chỉ có vài dịch vụ, để LLM vẫn có gì đó tham khảo).
    scored_services = [
        s
        for s in ranked_services
        if _score(expanded_tokens, s.name, device.brand, device.model) > 0
    ] or ranked_services
    scored_parts = [
        p
        for p in ranked_parts
        if _score(expanded_tokens, p.name, device.brand, device.model) > 0
    ] or ranked_parts

    # 5) Giới hạn số candidate + lưu điểm để lớp sau chọn nhóm liên quan nhất.
    final_services = scored_services[:MAX_CANDIDATES]
    final_parts = scored_parts[:MAX_CANDIDATES]

    return RetrievalResult(
        candidate_services=final_services,
        candidate_parts=final_parts,
        service_scores={
            service.service_id: _score(
                expanded_tokens, service.name, device.brand, device.model
            )
            for service in final_services
        },
        part_scores={
            part.part_id: _score(expanded_tokens, part.name, device.brand, device.model)
            for part in final_parts
        },
    )
