namespace RepairShop.Application.Common.Interfaces;

public record CatalogServiceItem(
    string ServiceId,
    string Name,
    string DeviceType,
    decimal BasePrice,
    string? Description = null);

/// <summary>
/// Task 3.1 (MAS): bổ sung thông tin tương thích + tồn kho để AI không gợi ý linh kiện
/// sai loại máy hoặc đã hết hàng. Các field mới có giá trị mặc định để không phá call-site cũ.
/// </summary>
public record CatalogPartItem(
    string PartId,
    string Name,
    decimal UnitPrice,
    string? CompatibleDeviceType = null,
    bool InStock = true,
    int QuantityOnHand = 0);

/// <summary>
/// Trừu tượng hoá việc lấy dữ liệu Service/Part hiện có — Infrastructure dùng interface này
/// để đóng gói "context" gửi sang FastAPI, KHÔNG tự query DbContext bên trong AIServiceClient
/// (giữ đúng ranh giới Repository).
/// </summary>
public interface IServiceCatalogQueryService
{
    Task<List<CatalogServiceItem>> GetAvailableServicesAsync(string deviceType);

    Task<List<CatalogPartItem>> GetAvailablePartsAsync();

    /// <summary>
    /// Lấy linh kiện đang bán, còn hàng và tương thích với loại thiết bị.
    /// Dùng cho MAS context; giữ overload không tham số cho luồng legacy.
    /// </summary>
    Task<List<CatalogPartItem>> GetAvailablePartsAsync(string? deviceType);
}
