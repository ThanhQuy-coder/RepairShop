using RepairShop.Domain.Modules.Tickets;

namespace RepairShop.Application.Common.Interfaces;

public interface IRepairStatusRepository
{
    Task<RepairStatus> GetByCodeAsync(string code);
}