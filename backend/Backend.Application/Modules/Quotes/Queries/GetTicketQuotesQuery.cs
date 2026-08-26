using MediatR;
using RepairShop.Application.Common.Authorization;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Quotes;
using RepairShop.Domain.Common;

public record GetTicketQuotesQuery(Guid TicketId) : IRequest<List<QuoteResponse>>;

public class GetTicketQuotesQueryHandler : IRequestHandler<GetTicketQuotesQuery, List<QuoteResponse>>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICurrentUserService _currentUser;

    public GetTicketQuotesQueryHandler(IRepairTicketRepository ticketRepository,
        ICustomerRepository customerRepository, ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _customerRepository = customerRepository;
        _currentUser = currentUser;
    }

    public async Task<List<QuoteResponse>> Handle(GetTicketQuotesQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        if (_currentUser.Role == Roles.Customer)
        {
            var customer = await _customerRepository.GetByUserIdAsync(_currentUser.UserId!.Value);
            TicketAccessGuard.EnsureCustomerOwnsTicket(ticket, customer); // <- bổ sung mới ở task này
        }

        return ticket.Quotes.Select(QuoteMapper.ToResponse).ToList();
    }
}