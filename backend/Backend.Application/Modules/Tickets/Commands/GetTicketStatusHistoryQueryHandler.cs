using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;

public class GetTicketStatusHistoryQueryHandler : IRequestHandler<GetTicketStatusHistoryQuery, List<StatusHistoryResponse>>
{
    private readonly IRepairTicketRepository _ticketRepository;

    public GetTicketStatusHistoryQueryHandler(IRepairTicketRepository ticketRepository) =>
        _ticketRepository = ticketRepository;

    public async Task<List<StatusHistoryResponse>> Handle(GetTicketStatusHistoryQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        // RepairTicketStatusHistory chỉ lưu ToStatus (StatusId) — không lưu FromStatus riêng, vì
        // FromStatus luôn CHÍNH LÀ ToStatus của bản ghi liền trước (mỗi bản ghi tương ứng đúng 1 transition
        // đã xảy ra qua ChangeStatus()). Sắp theo thời gian rồi suy ra, tránh lưu trùng lặp dữ liệu
        // đã có thể tính toán được (bản ghi đầu tiên không có FromStatus vì đó là lúc khởi tạo, không phải "chuyển từ").
        var ordered = ticket.StatusHistories.OrderBy(h => h.ChangedAt).ToList();

        var result = new List<StatusHistoryResponse>();
        for (var i = 0; i < ordered.Count; i++)
        {
            var current = ordered[i];
            var fromStatus = i == 0 ? null : ordered[i - 1].Status.Code;

            result.Add(new StatusHistoryResponse(
                ticket.Id, fromStatus, current.Status.Code,
                current.ChangedByUser.FullName, current.ChangedAt, current.Note));
        }

        return result;
    }
}