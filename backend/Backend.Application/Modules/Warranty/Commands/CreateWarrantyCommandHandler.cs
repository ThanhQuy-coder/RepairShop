using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;

public class CreateWarrantyCommandHandler : IRequestHandler<CreateWarrantyCommand, WarrantyResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly IWarrantyCodeGenerator _codeGenerator;

    public CreateWarrantyCommandHandler(IRepairTicketRepository ticketRepository, IWarrantyCodeGenerator codeGenerator)
    {
        _ticketRepository = ticketRepository;
        _codeGenerator = codeGenerator;
    }

    public async Task<WarrantyResponse> Handle(CreateWarrantyCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        var code = await _codeGenerator.GenerateUniqueCodeAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var endDate = startDate.AddMonths(request.WarrantyMonths);

        // Domain tự enforce: chỉ tạo được khi ticket đã DELIVERED, chưa từng có Warranty (Bước 2)
        var warranty = ticket.CreateWarranty(code, startDate, endDate, request.Terms);
        _ticketRepository.TrackNewWarranty(warranty);

        await _ticketRepository.SaveChangesAsync();

        return new WarrantyResponse(warranty.WarrantyCode, ticket.Id, warranty.StartDate, warranty.EndDate,
            warranty.Terms, warranty.Status.ToString(), warranty.IsExpired());
    }
}