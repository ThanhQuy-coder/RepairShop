using MediatR;
using RepairShop.Application.Common.Authorization;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Common;

public class GetWarrantyByTicketQueryHandler : IRequestHandler<GetWarrantyByTicketQuery, WarrantyResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICurrentUserService _currentUser;

    public GetWarrantyByTicketQueryHandler(IRepairTicketRepository ticketRepository,
        ICustomerRepository customerRepository, ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _customerRepository = customerRepository;
        _currentUser = currentUser;
    }

    public async Task<WarrantyResponse> Handle(GetWarrantyByTicketQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        if (_currentUser.Role == Roles.Customer)
        {
            var customer = await _customerRepository.GetByUserIdAsync(_currentUser.UserId!.Value);
            TicketAccessGuard.EnsureCustomerOwnsTicket(ticket, customer);
        }

        var warranty = ticket.Warranty
            ?? throw new NotFoundException("Thông tin bảo hành của ticket", request.TicketId);

        return new WarrantyResponse(warranty.WarrantyCode, ticket.Id, warranty.StartDate, warranty.EndDate,
            warranty.Terms, warranty.Status.ToString(), warranty.IsExpired());
    }
}