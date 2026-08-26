using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Tickets.DTOs;
using RepairShop.Domain.Common;
using MediatR;

namespace RepairShop.Application.Modules.Tickets.Queries;

public class TrackTicketByCodeQueryHandler : IRequestHandler<TrackTicketByCodeQuery, PublicTicketTrackingResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;

    // Map Code -> tên tiếng Việt thân thiện, KHÔNG lộ nguyên văn hằng số kỹ thuật ra khách hàng
    private static readonly Dictionary<string, string> StatusLabels = new()
    {
        [RepairStatusCodes.CheckedIn] = "Đã tiếp nhận",
        [RepairStatusCodes.Assigned] = "Đã phân công kỹ thuật viên",
        [RepairStatusCodes.Diagnosing] = "Đang kiểm tra",
        [RepairStatusCodes.WaitingApproval] = "Chờ khách xác nhận báo giá",
        [RepairStatusCodes.OnHold] = "Tạm hoãn",
        [RepairStatusCodes.WaitingParts] = "Chờ linh kiện",
        [RepairStatusCodes.InRepair] = "Đang sửa chữa",
        [RepairStatusCodes.QaTesting] = "Đang kiểm thử",
        [RepairStatusCodes.ReadyForPickup] = "Sẵn sàng bàn giao",
        [RepairStatusCodes.Delivered] = "Đã bàn giao",
        [RepairStatusCodes.ClosedRejected] = "Đã đóng (từ chối báo giá)"
    };

    public TrackTicketByCodeQueryHandler(IRepairTicketRepository ticketRepository) =>
        _ticketRepository = ticketRepository;

    public async Task<PublicTicketTrackingResponse> Handle(TrackTicketByCodeQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByTicketCodeForTrackingAsync(request.TicketCode)
            // Cố tình dùng message CHUNG CHUNG, không phân biệt "không tồn tại" khác "tồn tại nhưng bạn
            // không có quyền xem" — vì đây là endpoint PUBLIC, không nên tiết lộ ticket code có tồn tại
            // hay không cho người dò mã ngẫu nhiên (tương tự lý do Login dùng chung message ở Task 6).
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketCode);

        var history = ticket.StatusHistories
            .OrderBy(h => h.ChangedAt)
            .Select(h => new PublicStatusHistoryItem(
                h.Status.Code,
                StatusLabels.GetValueOrDefault(h.Status.Code, h.Status.Name),
                h.ChangedAt))
            .ToList();
        // Chủ động KHÔNG map h.ChangedByUser hay h.Note vào response — 2 field này thuộc dữ liệu nội bộ
        // (ai xử lý, ghi chú kỹ thuật) không phải thứ khách hàng cần biết.

        return new PublicTicketTrackingResponse(
            ticket.TicketCode,
            $"{ticket.Device.Brand} {ticket.Device.Model}", // KHÔNG trả SerialNumber (định danh nhạy cảm)
            ticket.Status.Code,
            StatusLabels.GetValueOrDefault(ticket.Status.Code, ticket.Status.Name),
            history,
            null); // EstimatedCompletion: chưa có logic ước lượng thời gian -> để null, KHÔNG bịa số liệu
    }
}