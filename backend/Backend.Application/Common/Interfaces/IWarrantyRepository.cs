namespace RepairShop.Application.Common.Interfaces;

public record CustomerWarrantyView(Domain.Modules.Warranty.Warranty Warranty, Guid TicketId, string TicketCode, string DeviceLabel);
public record StaffWarrantyLookupView(
    Domain.Modules.Warranty.Warranty Warranty, Guid TicketId, string TicketCode,
    Guid CustomerId, string CustomerName, string CustomerPhone, Guid DeviceId, string DeviceLabel);

public interface IWarrantyRepository
{
    Task<List<CustomerWarrantyView>> GetByCustomerIdAsync(Guid customerId);
    Task<List<StaffWarrantyLookupView>> SearchForStaffAsync(
        string? phone, string? warrantyCode, CancellationToken cancellationToken = default);
}