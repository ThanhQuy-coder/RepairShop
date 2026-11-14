namespace RepairShop.Application.Common.Interfaces;

public record CustomerWarrantyView(Domain.Modules.Warranty.Warranty Warranty, Guid TicketId, string TicketCode, string DeviceLabel);
public record PublicWarrantyLookupView(
    Domain.Modules.Warranty.Warranty Warranty,
    string TicketCode,
    string DeviceLabel,
    string? PackageName);
public record StaffWarrantyLookupView(
    Domain.Modules.Warranty.Warranty Warranty, Guid TicketId, string TicketCode,
    Guid CustomerId, string CustomerName, string CustomerPhone, Guid DeviceId, string DeviceLabel);

public interface IWarrantyRepository
{
    Task<List<CustomerWarrantyView>> GetByCustomerIdAsync(Guid customerId);
    Task<PublicWarrantyLookupView?> GetByWarrantyCodeForPublicAsync(
        string warrantyCode, CancellationToken cancellationToken = default);
    Task<List<StaffWarrantyLookupView>> SearchForStaffAsync(
        string? phone, string? warrantyCode, CancellationToken cancellationToken = default);
}