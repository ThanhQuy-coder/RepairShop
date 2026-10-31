using Microsoft.EntityFrameworkCore;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Modules.Inspections;

namespace RepairShop.Infrastructure.Persistence.Repositories;

public sealed class InspectionRepository : IInspectionRepository
{
    private readonly AppDbContext _context;

    public InspectionRepository(AppDbContext context) => _context = context;

    public Task<List<InspectionTemplate>> GetTemplatesAsync(DeviceType deviceType, InspectionType type)
    {
        var templates = InspectionCatalog.Items
            .Where(x => x.Type == type && (x.DeviceType is null || x.DeviceType == deviceType))
            .OrderBy(x => x.SortOrder)
            .Select(x => new InspectionTemplate(x.Name, x.DeviceType, x.Type, x.SortOrder))
            .ToList();
        return Task.FromResult(templates);
    }

    public Task<InspectionResult?> GetResultAsync(Guid ticketId, InspectionType type) =>
        _context.InspectionResults.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.TicketId == ticketId && x.InspectionType == type);

    public async Task AddResultAsync(InspectionResult result) => await _context.InspectionResults.AddAsync(result);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();

    private sealed record CatalogItem(string Name, DeviceType? DeviceType, InspectionType Type, int SortOrder);

    private static class InspectionCatalog
    {
        private static readonly string[] Appearance =
        [
            "Màn hình / Mặt kính", "Khung viền / Vỏ thiết bị", "Mặt lưng",
            "Ốc vít / Vị trí lắp ráp", "Tem niêm phong / Dấu hiệu tháo mở",
            "Dấu hiệu vào nước / Oxy hóa"
        ];

        private static readonly IReadOnlyList<CatalogItem> DeviceItems =
            new Dictionary<DeviceType, string[]>
            {
                [DeviceType.Phone] = ["Màn hình hiển thị", "Cảm ứng", "Độ sáng", "Camera trước", "Camera sau", "Loa ngoài", "Loa trong", "Micro", "Wi-Fi", "Bluetooth", "SIM / Sóng di động", "Nút nguồn", "Nút âm lượng", "Cổng sạc", "Rung"],
                [DeviceType.Laptop] = ["Màn hình", "Bàn phím", "Touchpad", "Webcam", "Micro", "Loa", "Wi-Fi", "Bluetooth", "Pin", "Bộ sạc", "Cổng USB", "Cổng kết nối khác", "Nút nguồn"],
                [DeviceType.Electronics] = ["Nguồn điện", "Nút điều khiển", "Màn hình / Đèn hiển thị (nếu có)", "Chức năng hoạt động chính", "Kết nối", "Cổng kết nối", "Âm thanh (nếu có)"],
                [DeviceType.Tablet] = ["Màn hình hiển thị", "Cảm ứng", "Camera trước", "Camera sau", "Loa", "Micro", "Wi-Fi", "Bluetooth", "SIM (nếu có)", "Nút nguồn", "Nút âm lượng", "Cổng sạc"],
                [DeviceType.Smartwatch] = ["Màn hình", "Cảm ứng", "Nút bấm", "Cảm biến", "Bluetooth", "Kết nối với điện thoại", "Sạc", "Rung", "Chức năng theo dõi cơ bản"]
            }.SelectMany(pair => pair.Value.Select((name, index) =>
                new CatalogItem(name, pair.Key, InspectionType.Functional, index))).ToList()
            .Concat(Appearance.Select((name, index) =>
                new CatalogItem(name, null, InspectionType.Appearance, index))).ToList();

        public static IEnumerable<CatalogItem> Items => DeviceItems;
    }
}
