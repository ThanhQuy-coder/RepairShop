using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Modules.Tickets;
using Microsoft.EntityFrameworkCore;

namespace RepairShop.Infrastructure.Persistence.Repositories;

public class RepairStatusRepository : IRepairStatusRepository
{
    private readonly AppDbContext _context;

    public RepairStatusRepository(AppDbContext context) => _context = context;

    public async Task<RepairStatus> GetByCodeAsync(string code) =>
        await _context.RepairStatuses.FirstOrDefaultAsync(s => s.Code == code)
            ?? throw new InvalidOperationException(
                $"RepairStatus với Code '{code}' chưa được seed trong database.");
}