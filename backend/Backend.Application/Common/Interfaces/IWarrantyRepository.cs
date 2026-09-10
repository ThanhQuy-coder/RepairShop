namespace RepairShop.Application.Common.Interfaces;

public record CustomerWarrantyView(Domain.Modules.Warranty.Warranty Warranty, Guid TicketId, string TicketCode, string DeviceLabel);

public interface IWarrantyRepository
{
    Task<List<CustomerWarrantyView>> GetByCustomerIdAsync(Guid customerId);
}