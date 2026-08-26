namespace RepairShop.Application.Modules.Tickets.DTOs;

/// <summary>
/// DTO whitelist riêng cho tra cứu công khai (FR-029). Chỉ khai báo field được phép lộ ra ngoài —
/// KHÔNG tái sử dụng TicketResponse nội bộ để tránh rò rỉ field nhạy cảm khi TicketResponse có
/// thêm property mới trong tương lai mà quên rà soát lại chỗ này.
/// </summary>
public record PublicTicketTrackingResponse(
    string TicketCode,
    string DeviceLabel,        // "iPhone 13" - gộp Brand + Model, không lộ SerialNumber
    string Status,
    string StatusLabel,        // tên trạng thái tiếng Việt, thân thiện hơn code
    List<PublicStatusHistoryItem> StatusHistory,
    DateTime? EstimatedCompletion);

public record PublicStatusHistoryItem(string Status, string StatusLabel, DateTime ChangedAt);