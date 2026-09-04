namespace RepairShop.Application.Modules.Tickets.DTOs;

// DTO riêng cho danh sách — nhẹ hơn TicketResponse (chi tiết), khớp đúng cột mentor yêu cầu:
// TicketCode / Customer / Device / Technician / Status / CreatedAt
public record TicketListItemResponse(
    Guid Id,
    string TicketCode,
    string CustomerName,
    string DeviceLabel,
    string? TechnicianName,
    string Status,
    DateTime ReceivedAt,
    string? IssueReported = null);

public record TicketListResponse(List<TicketListItemResponse> Items, int Total);