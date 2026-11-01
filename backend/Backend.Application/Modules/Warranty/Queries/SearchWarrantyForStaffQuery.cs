using MediatR;
using RepairShop.Application.Common.Interfaces;

namespace RepairShop.Application.Modules.Warranty.Queries;

public sealed record StaffWarrantyLookupResponse(
    string WarrantyCode, Guid TicketId, string TicketCode, Guid CustomerId,
    string CustomerName, string CustomerPhone, Guid DeviceId, string DeviceLabel,
    DateOnly StartDate, DateOnly EndDate, string Status);

public sealed record SearchWarrantyForStaffQuery(string? Phone, string? WarrantyCode)
    : IRequest<List<StaffWarrantyLookupResponse>>;

public sealed class SearchWarrantyForStaffQueryHandler(IWarrantyRepository repository)
    : IRequestHandler<SearchWarrantyForStaffQuery, List<StaffWarrantyLookupResponse>>
{
    public async Task<List<StaffWarrantyLookupResponse>> Handle(
        SearchWarrantyForStaffQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Phone) && string.IsNullOrWhiteSpace(request.WarrantyCode))
            throw new Domain.Common.Exceptions.DomainException("Vui lòng nhập số điện thoại hoặc mã bảo hành.");
        var rows = await repository.SearchForStaffAsync(
            request.Phone, request.WarrantyCode, cancellationToken);
        return rows.Select(x => new StaffWarrantyLookupResponse(
            x.Warranty.WarrantyCode, x.TicketId, x.TicketCode, x.CustomerId,
            x.CustomerName, x.CustomerPhone, x.DeviceId, x.DeviceLabel,
            x.Warranty.StartDate, x.Warranty.EndDate, x.Warranty.Status.ToString())).ToList();
    }
}
