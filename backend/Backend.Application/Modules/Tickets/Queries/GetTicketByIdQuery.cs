using MediatR;
using RepairShop.Application.Common.Authorization;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Tickets;
using RepairShop.Application.Modules.Tickets.DTOs;
using RepairShop.Domain.Common;

public record GetTicketByIdQuery(Guid TicketId) : IRequest<TicketResponse>;

public class GetTicketByIdQueryHandler : IRequestHandler<GetTicketByIdQuery, TicketResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICurrentUserService _currentUser;

    public GetTicketByIdQueryHandler(IRepairTicketRepository ticketRepository, ICustomerRepository customerRepository, ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _customerRepository = customerRepository;
        _currentUser = currentUser;
    }

    public async Task<TicketResponse> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new RepairShop.Application.Common.Exceptions.NotFoundException("Phiếu sửa chữa", request.TicketId);

        // Phân nhánh ownership theo role của người gọi:
        if (_currentUser.Role == Roles.Customer)
        {
            var customer = await _customerRepository.GetByUserIdAsync(_currentUser.UserId!.Value);
            TicketAccessGuard.EnsureCustomerOwnsTicket(ticket, customer); // Customer A xem Ticket B -> 403
        }
        else if (_currentUser.Role == Roles.Technician)
        {
            TicketAccessGuard.EnsureCanAccess(ticket, _currentUser); // Technician B xem Ticket của A -> 403
        }

        // Đây chính là chỗ enforce "Technician chỉ xem/thao tác ticket của mình" — Admin/Receptionist bỏ qua
        TicketAccessGuard.EnsureCanAccess(ticket, _currentUser);

        return TicketMapper.ToResponse(ticket);
    }
}