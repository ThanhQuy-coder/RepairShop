using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Tickets.DTOs;
using RepairShop.Domain.Common;
using MediatR;

namespace RepairShop.Application.Modules.Tickets.Queries;

public class GetTicketsQueryHandler : IRequestHandler<GetTicketsQuery, TicketListResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly ICurrentUserService _currentUser;

    public GetTicketsQueryHandler(IRepairTicketRepository ticketRepository, ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _currentUser = currentUser;
    }

    public async Task<TicketListResponse> Handle(GetTicketsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _ticketRepository.SearchAsync(
            request.Status, request.TechnicianId, request.CustomerId,
            _currentUser.UserId, _currentUser.Role, request.Page, request.PageSize);

        var mapped = items.Select(t => new TicketListItemResponse(
            t.Id, t.TicketCode, t.Customer.FullName, $"{t.Device.Brand} {t.Device.Model}",
            t.Technician?.FullName, t.Status.Code, t.ReceivedAt)).ToList();

        return new TicketListResponse(mapped, total);
    }
}