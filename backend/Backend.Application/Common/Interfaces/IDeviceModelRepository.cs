using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Modules.Devices;

namespace RepairShop.Application.Common.Interfaces;

public interface IDeviceModelRepository
{
    Task<DeviceModel?> GetByIdAsync(Guid id);
    Task<DeviceModel?> FindMatchAsync(DeviceType deviceType, string brandName, string modelName);
    Task<bool> ExistsAsync(Guid brandId, DeviceType deviceType, string modelName, Guid? excludeId = null);
    Task<bool> IsReferencedAsync(Guid deviceModelId);
    Task<(List<DeviceModel> Items, int Total)> SearchAsync(
        DeviceType? deviceType,
        Guid? brandId,
        bool? isActive,
        string? search,
        int page,
        int pageSize);
    Task AddAsync(DeviceModel model);
    void Update(DeviceModel model);
    Task SaveChangesAsync();
}
