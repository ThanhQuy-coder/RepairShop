using RepairShop.Domain.Modules.Customers;

namespace RepairShop.Application.Common.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id);
    Task<Customer?> GetByPhoneAsync(string phone);
    Task<(List<Customer> Items, int Total)> SearchAsync(string? search, int page, int pageSize);
    Task AddAsync(Customer customer);
    void Update(Customer customer);
    Task SaveChangesAsync();
    Task<Customer?> GetByUserIdAsync(Guid userId);
}