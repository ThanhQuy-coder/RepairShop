using Backend.Application.Common.Interfaces;
using Backend.Domain.Modules.Customers;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context) => _context = context;

    public Task<Customer?> GetByIdAsync(Guid id) =>
        _context.Customers.Include(c => c.Devices).FirstOrDefaultAsync(c => c.Id == id);

    public Task<Customer?> GetByPhoneAsync(string phone) =>
        _context.Customers.FirstOrDefaultAsync(c => c.Phone == phone);

    public async Task<(List<Customer> Items, int Total)> SearchAsync(string? search, int page, int pageSize)
    {
        var query = _context.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c =>
                c.FullName.Contains(search) || c.Phone.Contains(search)); // FR-010
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task AddAsync(Customer customer) => await _context.Customers.AddAsync(customer);

    public void Update(Customer customer) => _context.Customers.Update(customer);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}