"""Unit tests for Task 07: 3-Layer Part Compatibility & Out-of-Stock Filtering."""

from __future__ import annotations

from app.retrieval.retrieval_service import (
    is_part_brand_model_compatible,
    retrieve_candidates,
)
from app.schemas.request_models import (
    AdvisoryContext,
    AvailablePart,
    DeviceInfo,
    DeviceType,
)


def test_layer1_device_type_filters_wrong_device_parts() -> None:
    ctx = AdvisoryContext(
        availableServices=[],
        availableParts=[
            AvailablePart(
                partId="p-laptop-pin",
                name="Pin Dell Inspiron 3520",
                category="Pin",
                unitPrice=700000,
                compatibleDeviceType="laptop",
                inStock=True,
                quantityOnHand=5,
            ),
            AvailablePart(
                partId="p-phone-pin",
                name="Pin iPhone 13",
                category="Pin",
                unitPrice=350000,
                compatibleDeviceType="phone",
                inStock=True,
                quantityOnHand=5,
            ),
        ],
    )
    device = DeviceInfo(deviceType=DeviceType.PHONE, brand="Apple", model="iPhone 13")
    res = retrieve_candidates(device, "Pin tụt nhanh", ctx)
    ids = [p.part_id for p in res.candidate_parts]
    assert "p-phone-pin" in ids
    assert "p-laptop-pin" not in ids


def test_layer2_category_relevance_and_zero_quantity_stock_separation() -> None:
    ctx = AdvisoryContext(
        availableServices=[],
        availableParts=[
            AvailablePart(
                partId="p-pin-oos",
                name="Pin iPhone 13 Zin",
                category="Pin",
                unitPrice=450000,
                compatibleDeviceType="phone",
                inStock=True,
                quantityOnHand=0,  # out of stock even if inStock flag wasn't updated
            ),
            AvailablePart(
                partId="p-cam-in-stock",
                name="Cụm camera iPhone 13",
                category="Camera",
                unitPrice=800000,
                compatibleDeviceType="phone",
                inStock=True,
                quantityOnHand=4,
            ),
        ],
    )
    device = DeviceInfo(deviceType=DeviceType.PHONE, brand="Apple", model="iPhone 13")
    res = retrieve_candidates(device, "Pin chai nhanh hết", ctx)
    assert res.candidate_parts == []
    assert [p.part_id for p in res.out_of_stock_parts] == ["p-pin-oos"]


def test_layer3_strict_brand_and_model_compatibility() -> None:
    assert is_part_brand_model_compatible("Pin iPhone 13", "Apple", "iPhone 13") is True
    assert is_part_brand_model_compatible("Cáp sạc iPhone", "Apple", "iPhone 13") is True
    assert is_part_brand_model_compatible("Pin iPhone 14 Pro", "Apple", "iPhone 13") is False
    assert is_part_brand_model_compatible("Pin Samsung Galaxy S23 Ultra", "Apple", "iPhone 13") is False

    ctx = AdvisoryContext(
        availableServices=[],
        availableParts=[
            AvailablePart(
                partId="p-ip14",
                name="Pin iPhone 14 Pro",
                unitPrice=500000,
                compatibleDeviceType="phone",
                inStock=True,
            ),
            AvailablePart(
                partId="p-s23",
                name="Pin Samsung Galaxy S23 Ultra",
                unitPrice=450000,
                compatibleDeviceType="phone",
                inStock=True,
            ),
            AvailablePart(
                partId="p-ip13",
                name="Pin iPhone 13",
                unitPrice=350000,
                compatibleDeviceType="phone",
                inStock=True,
            ),
        ],
    )
    device = DeviceInfo(deviceType=DeviceType.PHONE, brand="Apple", model="iPhone 13")
    res = retrieve_candidates(device, "Pin tụt nhanh", ctx)
    assert [p.part_id for p in res.candidate_parts] == ["p-ip13"]
