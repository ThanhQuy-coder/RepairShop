using Microsoft.EntityFrameworkCore;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Modules.SLA;

namespace RepairShop.Infrastructure.Persistence.Repositories;

public sealed class SlaPolicyRepository(AppDbContext context) : ISlaPolicyRepository
{
    public async Task<IReadOnlyList<SLAPolicy>> GetAllAsync(CancellationToken cancellationToken)
        => await context.SLAPolicies.AsNoTracking()
            .OrderBy(policy => policy.StatusCode)
            .ThenBy(policy => policy.DeviceType)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(string statusCode, DeviceType? deviceType,
        CancellationToken cancellationToken)
        => context.SLAPolicies.AnyAsync(policy =>
            policy.StatusCode == statusCode.Trim().ToUpperInvariant() &&
            policy.DeviceType == deviceType, cancellationToken);

    public Task<SLAPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => context.SLAPolicies.SingleOrDefaultAsync(policy => policy.Id == id, cancellationToken);

    public Task AddAsync(SLAPolicy policy, CancellationToken cancellationToken)
        => context.SLAPolicies.AddAsync(policy, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);
}
