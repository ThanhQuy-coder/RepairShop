namespace RepairShop.Application.Modules.Tickets.DTOs;

public record TicketResponse(
    Guid Id,
    string TicketCode,
    Guid CustomerId,
    Guid DeviceId,
    string Status,
    string IssueReported,
    string? Notes,
    string? ConditionNotes,
    string? RiskWarning,
    DateTime ReceivedAt,
    string? CustomerName = null,
    string? CustomerPhone = null,
    string? DeviceType = null,
    string? DeviceBrand = null,
    string? DeviceModel = null,
    string? DeviceSerialNumber = null,
    string? DiagnosisResult = null,
    string? RootCause = null,
    string? RecommendedRepair = null,
    string? RequiredPartsNote = null,
    string? CompletionNotes = null,
    List<TicketImageResponse>? Images = null,
    List<TicketPartResponse>? UsedParts = null,
    TicketInvoiceResponse? Invoice = null);

public record TicketImageResponse(Guid Id, string ImageUrl, string ImageType, DateTime UploadedAt);
public record TicketPartResponse(Guid TicketPartId, string PartName, int Quantity, decimal UnitPriceAtUse, decimal Subtotal);
public record TicketInvoiceResponse(Guid Id, decimal TotalAmount, string PaymentMethod, DateTime? PaidAt, DateTime CreatedAt);