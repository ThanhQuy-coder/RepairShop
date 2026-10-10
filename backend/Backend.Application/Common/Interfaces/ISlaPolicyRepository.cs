using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Modules.SLA;

namespace RepairShop.Application.Common.Interfaces;

public interface ISlaPolicyRepository
{
    Task<IReadOnlyList<SLAPolicy>> GetAllAsync(CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string statusCode, DeviceType? deviceType, CancellationToken cancellationToken);
    Task<SLAPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(SLAPolicy policy, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
