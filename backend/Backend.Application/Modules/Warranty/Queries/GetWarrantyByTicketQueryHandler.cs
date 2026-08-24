using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;

public class GetWarrantyByTicketQueryHandler : IRequestHandler<GetWarrantyByTicketQuery, WarrantyResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;

    public GetWarrantyByTicketQueryHandler(IRepairTicketRepository ticketRepository) =>
        _ticketRepository = ticketRepository;

    public async Task<WarrantyResponse> Handle(GetWarrantyByTicketQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        var warranty = ticket.Warranty
            ?? throw new NotFoundException("Thông tin bảo hành của ticket", request.TicketId);

        return new WarrantyResponse(warranty.WarrantyCode, ticket.Id, warranty.StartDate, warranty.EndDate,
            warranty.Terms, warranty.Status.ToString(), warranty.IsExpired());
    }
}