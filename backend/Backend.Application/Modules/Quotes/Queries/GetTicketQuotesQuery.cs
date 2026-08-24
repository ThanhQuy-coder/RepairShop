using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Quotes;

public record GetTicketQuotesQuery(Guid TicketId) : IRequest<List<QuoteResponse>>;

public class GetTicketQuotesQueryHandler : IRequestHandler<GetTicketQuotesQuery, List<QuoteResponse>>
{
    private readonly IRepairTicketRepository _ticketRepository;

    public GetTicketQuotesQueryHandler(IRepairTicketRepository ticketRepository) =>
        _ticketRepository = ticketRepository;

    public async Task<List<QuoteResponse>> Handle(GetTicketQuotesQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        return ticket.Quotes.Select(QuoteMapper.ToResponse).ToList();
    }
}