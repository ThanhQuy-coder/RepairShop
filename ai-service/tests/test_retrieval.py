from app.retrieval.retrieval_service import retrieve_candidates
from app.schemas.request_models import (
    AdvisoryContext,
    AvailablePart,
    AvailableService,
    DeviceInfo,
    DeviceType,
)


def _context():
    return AdvisoryContext(
        availableServices=[
            AvailableService(
                serviceId="svc-1", name="Thay pin", deviceType="phone", basePrice=400000
            ),
            AvailableService(
                serviceId="svc-2",
                name="Vệ sinh main",
                deviceType="phone",
                basePrice=150000,
            ),
            AvailableService(
                serviceId="svc-3",
                name="Thay màn hình laptop",
                deviceType="laptop",
                basePrice=1200000,
            ),
        ],
        availableParts=[
            AvailablePart(partId="part-1", name="Pin iPhone 13", unitPrice=350000),
            AvailablePart(partId="part-2", name="Cáp sạc iPhone", unitPrice=90000),
        ],
    )


def test_filters_out_services_of_different_device_type():
    device = DeviceInfo(deviceType=DeviceType.PHONE, brand="iPhone", model="13")
    result = retrieve_candidates(device, "pin tụt nhanh", _context())

    service_ids = [s.service_id for s in result.candidate_services]
    assert "svc-3" not in service_ids  # laptop service phải bị loại khi device là phone


def test_ranks_by_keyword_overlap():
    device = DeviceInfo(deviceType=DeviceType.PHONE, brand="iPhone", model="13")
    result = retrieve_candidates(device, "pin tụt nhanh, cần thay pin", _context())

    assert (
        result.candidate_services[0].service_id == "svc-1"
    )  # "Thay pin" khớp từ khóa "pin" tốt nhất
    assert (
        result.candidate_parts[0].part_id == "part-1"
    )  # "Pin iPhone 13" khớp tốt nhất


def test_symptom_lexicon_prioritizes_relevant_services_over_unrelated_ones():
    context = AdvisoryContext(
        availableServices=[
            AvailableService(
                serviceId="svc-1", name="Thay pin", deviceType="phone", basePrice=400000
            ),
            AvailableService(
                serviceId="svc-2",
                name="Vệ sinh main",
                deviceType="phone",
                basePrice=150000,
            ),
            AvailableService(
                serviceId="svc-3",
                name="Thay màn hình",
                deviceType="phone",
                basePrice=900000,
            ),
            AvailableService(
                serviceId="svc-4",
                name="Thay camera",
                deviceType="phone",
                basePrice=600000,
            ),
        ],
        availableParts=[
            AvailablePart(partId="part-1", name="Pin iPhone 13", unitPrice=350000),
        ],
    )
    device = DeviceInfo(deviceType=DeviceType.PHONE, brand="iPhone", model="13")

    result = retrieve_candidates(device, "Pin tụt nhanh, máy nóng khi sạc", context)

    top_ids = [s.service_id for s in result.candidate_services[:2]]
    assert "svc-1" in top_ids  # Thay pin
    assert "svc-2" in top_ids  # Vệ sinh main
    # Thay màn hình / Thay camera không liên quan triệu chứng -> phải xếp SAU
    assert result.candidate_services.index(
        next(s for s in result.candidate_services if s.service_id == "svc-1")
    ) < result.candidate_services.index(
        next(s for s in result.candidate_services if s.service_id == "svc-3")
    )
