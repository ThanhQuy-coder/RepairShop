using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Common.Authorization;
using RepairShop.Domain.Common;

public class GetTicketStatusHistoryQueryHandler : IRequestHandler<GetTicketStatusHistoryQuery, List<StatusHistoryResponse>>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICurrentUserService _currentUser;

    public GetTicketStatusHistoryQueryHandler(
        IRepairTicketRepository ticketRepository,
        ICustomerRepository customerRepository,
        ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _customerRepository = customerRepository;
        _currentUser = currentUser;
    }

    public async Task<List<StatusHistoryResponse>> Handle(GetTicketStatusHistoryQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        if (_currentUser.Role == Roles.Customer)
        {
            var customer = _currentUser.UserId is null
                ? null
                : await _customerRepository.GetByUserIdAsync(_currentUser.UserId.Value);
            TicketAccessGuard.EnsureCustomerOwnsTicket(ticket, customer);
        }
        else if (_currentUser.Role == Roles.Technician)
        {
            TicketAccessGuard.EnsureCanAccess(ticket, _currentUser);
        }

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