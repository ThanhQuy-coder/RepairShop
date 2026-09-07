using System.Text.Json.Serialization;

namespace RepairShop.Infrastructure.AI;

internal record AdvisoryHttpRequest(
    [property: JsonPropertyName("requestId")] Guid RequestId,
    [property: JsonPropertyName("device")] AdvisoryDeviceDto Device,
    [property: JsonPropertyName("issueDescription")] string IssueDescription,
    [property: JsonPropertyName("context")] AdvisoryContextDto Context);

internal record AdvisoryDeviceDto(
    [property: JsonPropertyName("deviceType")] string DeviceType,
    [property: JsonPropertyName("brand")] string Brand,
    [property: JsonPropertyName("model")] string Model);

internal record AdvisoryContextDto(
    [property: JsonPropertyName("availableServices")] List<AdvisoryServiceDto> AvailableServices,
    [property: JsonPropertyName("availableParts")] List<AdvisoryPartDto> AvailableParts);

internal record AdvisoryServiceDto(
    [property: JsonPropertyName("serviceId")] string ServiceId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("deviceType")] string DeviceType,
    [property: JsonPropertyName("basePrice")] decimal BasePrice);

internal record AdvisoryPartDto(
    [property: JsonPropertyName("partId")] string PartId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("unitPrice")] decimal UnitPrice);

internal record AdvisoryHttpResponse(
    [property: JsonPropertyName("requestId")] Guid RequestId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("suggestedServices")] List<SuggestedServiceDto> SuggestedServices,
    [property: JsonPropertyName("suggestedParts")] List<SuggestedPartDto> SuggestedParts,
    [property: JsonPropertyName("priceRange")] PriceRangeDto? PriceRange,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("disclaimer")] string? Disclaimer);

internal record SuggestedServiceDto(
    [property: JsonPropertyName("serviceId")] string ServiceId,
    [property: JsonPropertyName("serviceName")] string ServiceName,
    [property: JsonPropertyName("confidence")] string Confidence);

internal record SuggestedPartDto(
    [property: JsonPropertyName("partId")] string PartId,
    [property: JsonPropertyName("partName")] string PartName);

internal record PriceRangeDto(
    [property: JsonPropertyName("min")] decimal Min,
    [property: JsonPropertyName("max")] decimal Max);