using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Tickets.DTOs;
using RepairShop.Domain.Common;

namespace RepairShop.Application.Modules.Customers.Queries;

public record GetCustomerTicketsQuery(Guid CustomerId) : IRequest<List<TicketListItemResponse>>;

public class GetCustomerTicketsQueryHandler : IRequestHandler<GetCustomerTicketsQuery, List<TicketListItemResponse>>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICurrentUserService _currentUser;

    public GetCustomerTicketsQueryHandler(
        IRepairTicketRepository ticketRepository,
        ICustomerRepository customerRepository,
        ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _customerRepository = customerRepository;
        _currentUser = currentUser;
    }

    public async Task<List<TicketListItemResponse>> Handle(
        GetCustomerTicketsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Role == Roles.Customer)
        {
            var customer = _currentUser.UserId is null
                ? null
                : await _customerRepository.GetByUserIdAsync(_currentUser.UserId.Value);
            if (customer is null || customer.Id != request.CustomerId)
                throw new ForbiddenException("Bạn không có quyền xem lịch sử sửa chữa của khách hàng này.");
        }

        var tickets = await _ticketRepository.GetByCustomerForHistoryAsync(request.CustomerId);
        return tickets.Select(ticket => new TicketListItemResponse(
            ticket.Id,
            ticket.TicketCode,
            ticket.Customer.FullName,
            $"{ticket.Device.Brand} {ticket.Device.Model}",
            ticket.Technician?.FullName,
            ticket.Status.Code,
            ticket.ReceivedAt,
            ticket.IssueReported)).ToList();
    }
}
