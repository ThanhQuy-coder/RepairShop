using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Modules.Warranty;
using RepairShop.Domain.Common.Exceptions;

public class CreateWarrantyCommandHandler : IRequestHandler<CreateWarrantyCommand, WarrantyResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly IWarrantyCodeGenerator _codeGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWarrantyPackageRepository _packageRepository;

    public CreateWarrantyCommandHandler(IRepairTicketRepository ticketRepository,
        IWarrantyCodeGenerator codeGenerator, IUnitOfWork unitOfWork,
        IWarrantyPackageRepository packageRepository)
    {
        _ticketRepository = ticketRepository;
        _codeGenerator = codeGenerator;
        _unitOfWork = unitOfWork;
        _packageRepository = packageRepository;
    }

    public async Task<WarrantyResponse> Handle(CreateWarrantyCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);
        var package = await _packageRepository.GetByIdAsync(request.WarrantyPackageId)
            ?? throw new NotFoundException("Gói bảo hành", request.WarrantyPackageId);
        if (!package.IsActive)
            throw new DomainException("Gói bảo hành đã ngừng hoạt động.");

        var code = await _codeGenerator.GenerateUniqueCodeAsync();
        var startDate = DateOnly.FromDateTime(ticket.DeliveredAt ?? DateTime.UtcNow);
        var endDate = startDate.AddMonths(package.DurationMonths);

        Warranty? warranty = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            warranty = ticket.CreateWarranty(code, startDate, endDate, package.Terms, package.Id); // snapshot terms
            _ticketRepository.TrackNewWarranty(warranty);
            await _ticketRepository.SaveChangesAsync();
        }, cancellationToken);

        return new WarrantyResponse(warranty!.WarrantyCode, ticket.Id, warranty.StartDate, warranty.EndDate,
            warranty.Terms, warranty.Status.ToString(), warranty.IsExpired(), warranty.WarrantyPackageId);
    }
}