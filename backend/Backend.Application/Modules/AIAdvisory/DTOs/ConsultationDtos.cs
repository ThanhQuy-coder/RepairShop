namespace RepairShop.Application.Modules.AIAdvisory.DTOs;

/// <summary>
/// Request tư vấn MAS từ Frontend. Backend tự dựng context (services/parts/slots) rồi gọi
/// FastAPI — AI không truy cập DB, không nhận JWT người dùng.
/// </summary>
public record ConsultationRequest(
    string DeviceType,
    string Brand,
    string Model,
    string IssueDescription,
    DateOnly? RequestedDate = null,
    bool AllowQuotation = true,
    bool AllowAppointment = true,
    string? FullName = null,
    string? Phone = null,
    string ActorType = "GUEST",
    bool AllowWarranty = true,
    bool AllowTracking = true,
    string? TicketCode = null,
    string? WarrantyCode = null,
    Guid? UserId = null);

public record ConsultationPossibleCause(string Name, string Confidence);

public record ConsultationServiceItem(
    string ServiceId,
    string ServiceName,
    string Confidence,
    double? MatchingScore = null);

public record ConsultationPartItem(string PartId, string PartName, string Confidence);

public record ConsultationDiagnosis(
    List<ConsultationPossibleCause> PossibleCauses,
    List<ConsultationServiceItem> SuggestedServices,
    List<ConsultationPartItem> SuggestedParts,
    List<string> MissingInformation,
    string Explanation);

public record ConsultationQuotationItem(
    string ItemId,
    string ItemType,
    string Name,
    decimal UnitPrice,
    decimal LineTotal);

public record ConsultationQuotation(
    List<ConsultationQuotationItem> Items,
    decimal EstimatedTotal,
    string Currency,
    List<string> Assumptions,
    string Explanation);

public record ConsultationSlot(
    string TimeSlotId,
    string SlotStart,
    string SlotEnd,
    DateOnly AppointmentDate,
    int Available);

public record ConsultationBookingDraft(
    string? FullName,
    string? Phone,
    DateOnly? AppointmentDate,
    string? TimeSlotId,
    string DeviceType,
    string Brand,
    string Model,
    string? IssueDescription);

public record ConsultationAppointment(
    List<ConsultationSlot> SuggestedSlots,
    ConsultationBookingDraft? BookingDraft,
    List<string> MissingInformation,
    bool RequiresUserConfirmation,
    string Explanation);

public record ConsultationWarrantyPolicy(
    string PackageId,
    string Name,
    int DurationMonths,
    string Terms,
    string? Description = null);

public record ConsultationWarrantyRecord(
    string WarrantyCode,
    string TicketCode,
    string DeviceLabel,
    string Status,
    string StartDate,
    string EndDate,
    bool IsExpired,
    string? PackageName = null,
    string? Terms = null);

public record ConsultationWarranty(
    List<ConsultationWarrantyPolicy> Policies,
    List<ConsultationWarrantyRecord> Warranties,
    List<string> MissingInformation,
    bool RequiresVerification,
    string Explanation);

public record ConsultationTrackingHistoryItem(
    string Status,
    string StatusLabel,
    string ChangedAt);

public record ConsultationTrackingTicket(
    string TicketCode,
    string DeviceLabel,
    string Status,
    string StatusLabel,
    List<ConsultationTrackingHistoryItem> StatusHistory,
    string? EstimatedCompletion = null);

public record ConsultationTracking(
    List<ConsultationTrackingTicket> Tickets,
    List<string> MissingInformation,
    string Explanation);

/// <summary>
/// Response đã hậu kiểm. AI chỉ tư vấn: không có appointmentId/status xác nhận.
/// </summary>
public record ConsultationResponse(
    bool AiAvailable,
    string Status,
    string Intent,
    List<string> ExecutedAgents,
    ConsultationDiagnosis? Diagnosis,
    ConsultationQuotation? Quotation,
    ConsultationAppointment? Appointment,
    string Message,
    string Disclaimer,
    string? AiRequestId = null,
    ConsultationWarranty? Warranty = null,
    ConsultationTracking? Tracking = null,
    List<string>? MissingInformation = null)
{
    public const string DefaultDisclaimer =
        "Đây là thông tin tư vấn tham khảo. Kỹ thuật viên sẽ kiểm tra và xác nhận phương án sửa chữa thực tế.";

    public static ConsultationResponse Unavailable(string message) =>
        new(false, "ERROR", "UNKNOWN", [], null, null, null, message, DefaultDisclaimer);
}
