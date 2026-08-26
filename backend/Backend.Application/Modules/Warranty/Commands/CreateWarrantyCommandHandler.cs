using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Modules.Warranty;

public class CreateWarrantyCommandHandler : IRequestHandler<CreateWarrantyCommand, WarrantyResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly IWarrantyCodeGenerator _codeGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public CreateWarrantyCommandHandler(IRepairTicketRepository ticketRepository,
        IWarrantyCodeGenerator codeGenerator, IUnitOfWork unitOfWork)
    {
        _ticketRepository = ticketRepository;
        _codeGenerator = codeGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<WarrantyResponse> Handle(CreateWarrantyCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        var code = await _codeGenerator.GenerateUniqueCodeAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var endDate = startDate.AddMonths(request.WarrantyMonths);

        Warranty? warranty = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            warranty = ticket.CreateWarranty(code, startDate, endDate, request.Terms); // enforce DELIVERED (Task 4.14)
            _ticketRepository.TrackNewWarranty(warranty);
            await _ticketRepository.SaveChangesAsync();
        }, cancellationToken);

        return new WarrantyResponse(warranty!.WarrantyCode, ticket.Id, warranty.StartDate, warranty.EndDate,
            warranty.Terms, warranty.Status.ToString(), warranty.IsExpired());
    }
}