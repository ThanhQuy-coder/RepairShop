using MediatR;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Tickets.Commands;

public class GetTicketImagesQueryHandler : IRequestHandler<GetTicketImagesQuery, List<TicketImageResponse>>
{
    private readonly IRepairTicketRepository _ticketRepository;

    public GetTicketImagesQueryHandler(IRepairTicketRepository ticketRepository) =>
        _ticketRepository = ticketRepository;

    public async Task<List<TicketImageResponse>> Handle(GetTicketImagesQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new RepairShop.Application.Common.Exceptions.NotFoundException("Phiếu sửa chữa", request.TicketId);

        return ticket.Images
            .Select(i => new TicketImageResponse(i.Id, i.ImageUrl, i.ImageType.ToString(), i.UploadedAt))
            .ToList();
    }
}