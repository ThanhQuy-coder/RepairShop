"""Unit tests for Task 05: Vietnamese normalization, 74 symptoms sync, 12 Brand synonyms & boundary rules."""

from __future__ import annotations

from app.core.text import (
    BRAND_SYNONYM_MAP,
    SEEDED_DEVICE_MODELS,
    contains_term,
    contains_term_flexible,
    extract_brand_and_model,
    normalize_brand,
    normalize_colloquial_vietnamese,
    normalize_text,
)
from app.retrieval.symptom_lexicon import (
    CANONICAL_DIAGNOSIS_SYMPTOMS,
    expand_text_keywords,
    map_text_to_canonical_symptoms,
)


def test_canonical_symptom_catalog_has_74_records_across_5_device_types() -> None:
    counts = {k: len(v) for k, v in CANONICAL_DIAGNOSIS_SYMPTOMS.items()}
    assert counts == {
        "phone": 15,
        "laptop": 16,
        "electronics": 15,
        "tablet": 14,
        "smartwatch": 14,
    }
    assert sum(counts.values()) == 74


def test_normalize_text_preserves_vietnamese_diacritics_and_nfc() -> None:
    # Decomposed NFD input should normalize to NFC and stay lowercase without stripping tone marks
    decomposed = "Ma\u0300n hi\u0300nh   BI\u0323   V\u01A0\u0303"
    assert normalize_text(decomposed) == "màn hình bị vỡ"
    assert normalize_text("Đặt lịch bảo hành") != normalize_text("Dat lich bao hanh")


def test_colloquial_expansion_and_word_boundaries() -> None:
    assert normalize_colloquial_vietnamese("đt ip 13 ko lên mh") == "điện thoại iphone 13 không lên màn hình"
    assert contains_term("màn hình loạn cảm ứng", "cảm ứng") is True
    assert contains_term("màn hình loạn cảm ứng", "loa") is False  # 'loa' must not match inside 'loạn'
    assert contains_term("encoding utf-8", "code") is False
    assert contains_term_flexible("pin tut nhanh sac ko vao", "pin tụt") is True


def test_brand_synonym_map_covers_all_12_seeded_brands() -> None:
    assert set(BRAND_SYNONYM_MAP.keys()) == {
        "Apple", "Samsung", "Xiaomi", "Dell", "ASUS", "Lenovo",
        "HP", "Garmin", "Sony", "Marshall", "Nintendo", "LG",
    }
    assert len(SEEDED_DEVICE_MODELS) == 40
    assert normalize_brand("ip 15 pro max") == "Apple"
    assert normalize_brand("mb air m2") == "Apple"
    assert normalize_brand("ss galaxy s24") == "Samsung"
    assert normalize_brand("redmi note 13") == "Xiaomi"
    assert normalize_brand("forerunner 265") == "Garmin"
    assert normalize_brand("tay cầm ps5 dualsense") == "Sony"
    assert normalize_brand("loa stanmore iii") == "Marshall"
    assert normalize_brand("switch oled") == "Nintendo"
    assert normalize_brand("ultragear") == "LG"


def test_extract_brand_and_model_from_natural_text() -> None:
    brand, model = extract_brand_and_model("Con iphone 15 pro max cua em bi soc man hinh")
    assert brand == "Apple"
    assert model == "iPhone 15 Pro Max"

    brand2, model2 = extract_brand_and_model("asus rog strix g16 quat keu to")
    assert brand2 == "ASUS"
    assert model2 == "ASUS ROG Strix G16"


def test_symptom_lexicon_accented_unaccented_and_context_disambiguation() -> None:
    kw_acc = expand_text_keywords("Pin tụt nhanh và sọc màn hình")
    assert "pin" in kw_acc
    assert "màn hình" in kw_acc

    kw_no_acc = expand_text_keywords("pin tut nhanh va chay muc man hinh")
    assert "pin" in kw_no_acc
    assert "màn hình" in kw_no_acc

    # "nước ngoài" must NOT trigger liquid damage / repair keywords
    kw_false_positive = expand_text_keywords("Tôi sắp đi nước ngoài nên hỏi giờ mở cửa")
    assert "vệ sinh" not in kw_false_positive
    assert "vào nước" not in kw_false_positive

    mapped = map_text_to_canonical_symptoms("iPhone 13 pin tut nhanh va sac khong vao", "phone")
    assert "Pin nhanh hết" in mapped
    assert "Không sạc được" in mapped
