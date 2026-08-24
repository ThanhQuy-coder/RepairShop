using RepairShop.Domain.Modules.Billing;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id);
    Task SaveChangesAsync();
}