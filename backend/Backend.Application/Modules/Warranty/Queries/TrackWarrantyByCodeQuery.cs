using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Modules.Warranty.Enums;

namespace RepairShop.Application.Modules.Warranty.Queries;

public record PublicWarrantyTrackingResponse(
    string WarrantyCode,
    string TicketCode,
    string DeviceLabel,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    bool IsExpired,
    string? PackageName,
    string? Terms);

public record TrackWarrantyByCodeQuery(string WarrantyCode) : IRequest<PublicWarrantyTrackingResponse>;

public class TrackWarrantyByCodeQueryHandler
    : IRequestHandler<TrackWarrantyByCodeQuery, PublicWarrantyTrackingResponse>
{
    private readonly IWarrantyRepository _warrantyRepository;

    public TrackWarrantyByCodeQueryHandler(IWarrantyRepository warrantyRepository) =>
        _warrantyRepository = warrantyRepository;

    public async Task<PublicWarrantyTrackingResponse> Handle(
        TrackWarrantyByCodeQuery request,
        CancellationToken cancellationToken)
    {
        var view = await _warrantyRepository.GetByWarrantyCodeForPublicAsync(
            request.WarrantyCode, cancellationToken)
            ?? throw new NotFoundException("Bảo hành", request.WarrantyCode);

        var isExpired = view.Warranty.IsExpired();
        var status = view.Warranty.Status == WarrantyStatus.Voided
            ? "Voided"
            : isExpired
                ? "Expired"
                : "Active";

        return new PublicWarrantyTrackingResponse(
            view.Warranty.WarrantyCode,
            view.TicketCode,
            view.DeviceLabel,
            view.Warranty.StartDate,
            view.Warranty.EndDate,
            status,
            isExpired,
            view.PackageName,
            view.Warranty.Terms);
    }
}
