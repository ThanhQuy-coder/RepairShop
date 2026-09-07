namespace RepairShop.Application.Common.Interfaces;

public record CatalogServiceItem(string ServiceId, string Name, string DeviceType, decimal BasePrice);
public record CatalogPartItem(string PartId, string Name, decimal UnitPrice);

/// <summary>
/// Trừu tượng hoá việc lấy dữ liệu Service/Part hiện có — Infrastructure (AIServiceClient)
/// sẽ dùng interface này để đóng gói "context" gửi sang FastAPI, KHÔNG tự query DbContext
/// trực tiếp bên trong AIServiceClient (giữ đúng ranh giới Repository).
/// </summary>
public interface IServiceCatalogQueryService
{
    Task<List<CatalogServiceItem>> GetAvailableServicesAsync(string deviceType);
    Task<List<CatalogPartItem>> GetAvailablePartsAsync();
}