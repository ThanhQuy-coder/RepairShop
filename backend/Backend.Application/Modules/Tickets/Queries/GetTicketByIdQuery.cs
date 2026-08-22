using MediatR;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Tickets.DTOs;

public record GetTicketByIdQuery(Guid TicketId) : IRequest<TicketResponse>;

public class GetTicketByIdQueryHandler : IRequestHandler<GetTicketByIdQuery, TicketResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly ICurrentUserService _currentUser;

    public GetTicketByIdQueryHandler(IRepairTicketRepository ticketRepository, ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _currentUser = currentUser;
    }

    public async Task<TicketResponse> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new RepairShop.Application.Common.Exceptions.NotFoundException("Phiếu sửa chữa", request.TicketId);

        // Đây chính là chỗ enforce "Technician chỉ xem/thao tác ticket của mình" — Admin/Receptionist bỏ qua
        RepairShop.Application.Common.Authorization.TicketAccessGuard.EnsureCanAccess(ticket, _currentUser);

        return new TicketResponse(ticket.Id, ticket.TicketCode, ticket.CustomerId, ticket.DeviceId,
            ticket.Status.Code, ticket.IssueReported, ticket.Notes, ticket.ConditionNotes, ticket.RiskWarning,
            ticket.ReceivedAt);
    }
}