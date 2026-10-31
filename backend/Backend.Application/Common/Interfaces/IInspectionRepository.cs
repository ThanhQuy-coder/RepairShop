using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Modules.Inspections;

namespace RepairShop.Application.Common.Interfaces;

public interface IInspectionRepository
{
    Task<List<InspectionTemplate>> GetTemplatesAsync(DeviceType deviceType, InspectionType type);
    Task<InspectionResult?> GetResultAsync(Guid ticketId, InspectionType type);
    Task AddResultAsync(InspectionResult result);
    Task SaveChangesAsync();
}
