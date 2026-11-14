"""Service Matching (TASK-06) & 3-Layer Part Compatibility Retrieval (TASK-07).

Quy trình đối sánh cho Context thực tế từ Backend:
- Service Matching:
  1. Lọc theo DeviceType (chấp nhận đúng device_type hoặc "general").
  2. Chấm điểm trên name + description + mở rộng từ đồng nghĩa & triệu chứng chuẩn (có dấu / không dấu).
  3. Loại bỏ nhiễu từ động từ chung ("thay", "sửa") và phát hiện mô tả mơ hồ (`is_vague_symptom_description`).
  4. Chuẩn hóa `service_normalized_scores` trong [0.0, 1.0].
- 3-Layer Part Compatibility Filtering:
  1. Layer 1 (DeviceType): `compatible_device_type` phải khớp `device_type` hoặc rỗng/"general".
  2. Layer 2 (Category / Symptom relevance): Tên + Category linh kiện phải khớp từ khóa triệu chứng/hỏng hóc.
  3. Layer 3 (Brand & Model compatibility): Nếu thiết bị và linh kiện đều có brand/model thì phải tương thích.
  4. Stock gating: Linh kiện hết hàng (`inStock=False` hoặc `quantityOnHand==0`) tách riêng vào `out_of_stock_parts`.
"""

from __future__ import annotations

import re
from dataclasses import dataclass, field
from typing import List, Optional

from app.core.text import (
    contains_term_flexible,
    extract_brand_and_model,
    normalize_brand,
    normalize_colloquial_vietnamese,
    normalize_text,
    strip_vietnamese_diacritics,
)
from app.retrieval.symptom_lexicon import (
    expand_text_keywords,
    map_text_to_canonical_symptoms,
)
from app.schemas.request_models import (
    AdvisoryContext,
    AvailablePart,
    AvailableService,
    DeviceInfo,
)

MAX_CANDIDATES = 8
_BRAND_MODEL_BONUS = 2

# Generic verbs / nouns that must NOT by themselves trigger unrelated services/parts
_GENERIC_STOPWORDS: frozenset[str] = frozenset({
    "thay", "sua", "sửa", "chua", "chữa", "ve", "vệ", "sinh", "kiem", "kiểm", "tra",
    "may", "máy", "thiet", "thiết", "bi", "bị", "dien", "điện", "thoai", "thoại",
    "laptop", "tablet", "smartwatch", "dong", "đồng", "ho", "hồ", "cho", "xin",
    "hoi", "hỏi", "gia", "giá", "bao", "nhieu", "nhiêu", "tien", "tiền",
    "loi", "lỗi", "hu", "hư", "hong", "hỏng", "giup", "giúp", "tu", "tư", "van", "vấn",
    "can", "cần", "nhe", "nhé", "a", "ạ", "roi", "rồi", "cua", "của", "em", "minh", "mình",
    "toi", "tôi", "ban", "bạn", "shop", "ad", "cu", "cụ", "the", "thể", "bi", "va", "và",
    "khong", "không", "ko", "duoc", "được", "dc", "co", "có", "hay", "bi", "bị",
    "tuong", "tương", "thich", "thích", "chinh", "chính", "hang", "hãng", "cac", "các",
    "dich", "dịch", "vu", "vụ", "linh", "kien", "kiện", "cai", "cái", "con", "này", "nay",
})


@dataclass
class RetrievalResult:
    """Candidate đã lọc — CHỈ sắp xếp/lọc từ dữ liệu thật Backend gửi lên."""

    candidate_services: List[AvailableService] = field(default_factory=list)
    candidate_parts: List[AvailablePart] = field(default_factory=list)
    service_scores: dict[str, int] = field(default_factory=dict)
    part_scores: dict[str, int] = field(default_factory=dict)
    service_normalized_scores: dict[str, float] = field(default_factory=dict)
    part_normalized_scores: dict[str, float] = field(default_factory=dict)
    out_of_stock_parts: List[AvailablePart] = field(default_factory=list)
    matched_any_service: bool = False
    is_vague_issue: bool = False


def _tokenize(text: str) -> set[str]:
    return set(re.findall(r"[0-9a-zA-ZÀ-ỹĐđ]+", text.lower()))


def _meaningful_tokens(text: str) -> set[str]:
    norm = normalize_colloquial_vietnamese(text)
    raw = _tokenize(norm) | _tokenize(strip_vietnamese_diacritics(norm))
    return {tok for tok in raw if tok not in _GENERIC_STOPWORDS and len(tok) >= 2}


def is_vague_symptom_description(issue_description: str) -> bool:
    """True nếu mô tả chỉ toàn từ chung chung ("máy hư", "bị lỗi") thiếu triệu chứng/bộ phận."""
    if not issue_description or not issue_description.strip():
        return True
    if expand_text_keywords(issue_description):
        return False
    # Check meaningful non-brand component tokens left after stripping generic stopwords
    norm = normalize_colloquial_vietnamese(issue_description)
    remaining = _tokenize(norm) - _GENERIC_STOPWORDS
    # If only 0-1 trivial words remain and no symptom lexicon hits, it's vague.
    if not remaining:
        return True
    # Also remove pure brand words (e.g. "iPhone", "Samsung", "Dell") when checking vagueness
    brand = normalize_brand(norm)
    if brand:
        brand_words = _tokenize(brand) | {tok for tok in remaining if normalize_brand(tok) == brand}
        remaining = remaining - brand_words
    return len(remaining) == 0


def _strip_brand_prefix(model: str, brand: Optional[str]) -> str:
    low = normalize_text(model)
    if brand:
        b_low = normalize_text(brand)
        if low.startswith(b_low + " "):
            return low[len(b_low) + 1 :].strip()
        if b_low == "apple" and low.startswith("apple "):
            return low[6:].strip()
    return low


def is_part_brand_model_compatible(
    part_name: str,
    device_brand: str,
    device_model: str,
    issue_text: str = "",
) -> bool:
    """Layer 3 compatibility check between part name and target device (Brand + Model)."""
    target_brand, target_model = extract_brand_and_model(
        issue_text, hint_brand=device_brand, hint_model=device_model
    )
    part_brand, part_model = extract_brand_and_model(part_name)

    if target_brand and part_brand and target_brand.lower() != part_brand.lower():
        return False

    if target_model and part_model:
        norm_target = _strip_brand_prefix(target_model, target_brand)
        norm_part = _strip_brand_prefix(part_model, part_brand)
        if norm_target != norm_part:
            # Allow subset only if numbers match cleanly? Reject e.g. iphone 13 vs iphone 14,
            # galaxy s23 vs galaxy s24, macbook air vs macbook pro.
            return False

    return True


def _build_expanded_terms(
    issue_description: str,
    device_type: str,
) -> tuple[set[str], set[str], list[str]]:
    norm = normalize_colloquial_vietnamese(issue_description)
    base_tokens = _meaningful_tokens(norm)
    lexicon_kw = expand_text_keywords(issue_description)
    canonical_syms = map_text_to_canonical_symptoms(issue_description, device_type)

    phrases: list[str] = list(lexicon_kw) + list(canonical_syms)
    expanded: set[str] = set(base_tokens)
    for phrase in phrases:
        expanded.update(_meaningful_tokens(phrase))
    return base_tokens, expanded, phrases


def _score_service(
    service: AvailableService,
    base_tokens: set[str],
    expanded_tokens: set[str],
    phrases: list[str],
    device: DeviceInfo,
) -> int:
    name_norm = normalize_colloquial_vietnamese(service.name)
    desc_norm = normalize_colloquial_vietnamese(service.description or "")
    name_tokens = _meaningful_tokens(name_norm)
    desc_tokens = _meaningful_tokens(desc_norm)

    score = 0
    for phrase in phrases:
        p_norm = normalize_text(phrase)
        if not p_norm or p_norm in _GENERIC_STOPWORDS:
            continue
        if contains_term_flexible(name_norm, p_norm):
            score += 4 if " " in p_norm else 3
        elif desc_norm and contains_term_flexible(desc_norm, p_norm):
            score += 2

    score += 3 * len(base_tokens & name_tokens)
    score += 1 * len((expanded_tokens - base_tokens) & name_tokens)
    score += len(expanded_tokens & desc_tokens)

    if score == 0:
        return 0

    for val in (device.brand, device.model):
        for tok in _meaningful_tokens(val):
            if tok in name_tokens:
                score += _BRAND_MODEL_BONUS

    if service.device_type.lower() == device.device_type.value:
        score += 1

    return score


def _score_part(
    part: AvailablePart,
    base_tokens: set[str],
    expanded_tokens: set[str],
    phrases: list[str],
    device: DeviceInfo,
) -> int:
    name_norm = normalize_colloquial_vietnamese(part.name)
    cat_norm = normalize_colloquial_vietnamese(part.category or "")
    name_tokens = _meaningful_tokens(name_norm)
    cat_tokens = _meaningful_tokens(cat_norm)

    score = 0
    for phrase in phrases:
        p_norm = normalize_text(phrase)
        if not p_norm or p_norm in _GENERIC_STOPWORDS:
            continue
        if contains_term_flexible(name_norm, p_norm):
            score += 3
        elif cat_norm and contains_term_flexible(cat_norm, p_norm):
            score += 2

    score += 3 * len(base_tokens & name_tokens)
    score += 1 * len((expanded_tokens - base_tokens) & name_tokens)
    score += len(expanded_tokens & cat_tokens)

    if score == 0:
        return 0

    for val in (device.brand, device.model):
        for tok in _meaningful_tokens(val):
            if tok in name_tokens:
                score += _BRAND_MODEL_BONUS

    if part.compatible_device_type and part.compatible_device_type.lower() == device.device_type.value:
        score += 1

    return score


def retrieve_candidates(
    device: DeviceInfo,
    issue_description: str,
    context: AdvisoryContext,
) -> RetrievalResult:
    """Đối sánh dịch vụ & lọc linh kiện tương thích 3 lớp hoàn toàn từ dữ liệu Backend."""
    base_tokens, expanded_tokens, phrases = _build_expanded_terms(issue_description, device.device_type.value)
    vague = is_vague_symptom_description(issue_description)

    # Layer 1 Service: DeviceType (exact or "general")
    eligible_services = [
        s
        for s in context.available_services
        if s.device_type.lower() in (device.device_type.value, "general")
    ]
    service_raw: dict[str, int] = {
        s.service_id: _score_service(s, base_tokens, expanded_tokens, phrases, device)
        for s in eligible_services
    }
    ranked_services = sorted(
        eligible_services,
        key=lambda s: service_raw[s.service_id],
        reverse=True,
    )
    matched_services = [s for s in ranked_services if service_raw[s.service_id] > 0]
    matched_any_service = bool(matched_services)

    # Backward compat: keep ranked_services when nothing matched (scores remain 0 so
    # downstream Diagnosis/Quotation agents detect unmatched / vague queries accurately).
    selected_services = (matched_services or ranked_services)[:MAX_CANDIDATES]
    max_svc_raw = max((service_raw[s.service_id] for s in selected_services), default=0)
    denom_svc = max(6.0, float(max_svc_raw)) if max_svc_raw > 0 else 1.0

    # Layer 1 + Layer 2 + Layer 3 Part Compatibility
    compatible_parts_all: list[AvailablePart] = []
    part_raw: dict[str, int] = {}
    for p in context.available_parts:
        dev_compat = (p.compatible_device_type or "").lower()
        if dev_compat not in ("", "general", device.device_type.value):
            continue
        if not is_part_brand_model_compatible(p.name, device.brand, device.model, issue_description):
            continue
        p_score = _score_part(p, base_tokens, expanded_tokens, phrases, device)
        compatible_parts_all.append(p)
        part_raw[p.part_id] = p_score

    ranked_in_stock = sorted(
        [
            p
            for p in compatible_parts_all
            if p.in_stock and (p.quantity_on_hand is None or p.quantity_on_hand > 0)
        ],
        key=lambda p: part_raw[p.part_id],
        reverse=True,
    )
    matched_in_stock = [p for p in ranked_in_stock if part_raw[p.part_id] > 0]

    ranked_out_of_stock = sorted(
        [
            p
            for p in compatible_parts_all
            if not p.in_stock or (p.quantity_on_hand is not None and p.quantity_on_hand <= 0)
        ],
        key=lambda p: part_raw[p.part_id],
        reverse=True,
    )
    out_of_stock_matched = [p for p in ranked_out_of_stock if part_raw[p.part_id] > 0][:MAX_CANDIDATES]

    selected_parts = (
        matched_in_stock
        if (matched_in_stock or matched_any_service or out_of_stock_matched)
        else ranked_in_stock
    )[:MAX_CANDIDATES]
    max_part_raw = max((part_raw[p.part_id] for p in selected_parts), default=0)
    denom_part = max(5.0, float(max_part_raw)) if max_part_raw > 0 else 1.0

    return RetrievalResult(
        candidate_services=selected_services,
        candidate_parts=selected_parts,
        service_scores={s.service_id: service_raw[s.service_id] for s in selected_services},
        part_scores={p.part_id: part_raw[p.part_id] for p in selected_parts},
        service_normalized_scores={
            s.service_id: (round(min(1.0, service_raw[s.service_id] / denom_svc), 2) if service_raw[s.service_id] > 0 else 0.0)
            for s in selected_services
        },
        part_normalized_scores={
            p.part_id: (round(min(1.0, part_raw[p.part_id] / denom_part), 2) if part_raw[p.part_id] > 0 else 0.0)
            for p in selected_parts
        },
        out_of_stock_parts=out_of_stock_matched,
        matched_any_service=matched_any_service,
        is_vague_issue=vague,
    )
