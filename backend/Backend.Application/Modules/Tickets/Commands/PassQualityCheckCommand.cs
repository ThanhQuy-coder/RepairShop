using MediatR;
using RepairShop.Application.Modules.Tickets.DTOs;

public record PassQualityCheckCommand(
    Guid TicketId,
    string FunctionalCheckNotes,       // Chức năng thiết bị
    string CosmeticCheckNotes,         // Tình trạng ngoại hình
    string OriginalIssueResolvedNotes  // Các lỗi ban đầu đã khắc phục hay chưa
) : IRequest<TicketResponse>;