using Microsoft.EntityFrameworkCore;
using RepairShop.Domain.Modules.Content;

namespace RepairShop.Infrastructure.Persistence.Seeders;

public class ServiceCatalogSeeder : IDataSeeder
{
    private readonly AppDbContext _context;

    public ServiceCatalogSeeder(AppDbContext context) => _context = context;

    public int Order => 15;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingServices = await _context.Services
            .Where(s => s.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var seedDefs = GetSeedDefinitions();
        var existingByKey = existingServices
            .ToDictionary(
                s => $"{(s.DeviceType ?? "GENERAL").ToUpperInvariant()}:{s.Name.Trim().ToLowerInvariant()}",
                s => s);

        var hasChanges = false;

        // Backfill missing ImageUrl on existing seeded services without overwriting user modifications
        foreach (var def in seedDefs)
        {
            var key = $"{(def.DeviceType ?? "GENERAL").ToUpperInvariant()}:{def.Name.Trim().ToLowerInvariant()}";
            if (existingByKey.TryGetValue(key, out var existing))
            {
                if (string.IsNullOrWhiteSpace(existing.ImageUrl))
                {
                    existing.UpdateImage(def.ImageUrl);
                    hasChanges = true;
                }
                continue;
            }

            // Also check by Name only for the 8 legacy HasData services
            var legacyMatch = existingServices.FirstOrDefault(s =>
                string.Equals(s.Name.Trim(), def.Name.Trim(), StringComparison.OrdinalIgnoreCase));
            if (legacyMatch is not null)
            {
                if (string.IsNullOrWhiteSpace(legacyMatch.ImageUrl))
                {
                    legacyMatch.UpdateImage(def.ImageUrl);
                    hasChanges = true;
                }
                continue;
            }

            var newService = new Service(
                def.Name,
                def.Description,
                def.BasePrice,
                def.DeviceType,
                def.ImageUrl);

            await _context.Services.AddAsync(newService, cancellationToken);
            hasChanges = true;
        }

        if (hasChanges)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed record ServiceSeedDef(
        string Name,
        string Description,
        decimal BasePrice,
        string? DeviceType,
        string ImageUrl);

    private static IReadOnlyList<ServiceSeedDef> GetSeedDefinitions() =>
    [
        // Phone (5 services)
        new(
            "Thay pin điện thoại",
            "Thay pin dung lượng chuẩn chính hãng/OEM cho iPhone, Samsung, Xiaomi. Kiểm tra dòng rò và bảo hành 6–12 tháng.",
            350000m,
            "Phone",
            "/images/services/phone-battery.svg"),
        new(
            "Thay màn hình điện thoại",
            "Thay cụm màn hình OLED/AMOLED hiển thị sắc nét cho máy vỡ màn, chảy mực, sọc xanh hoặc liệt cảm ứng.",
            800000m,
            "Phone",
            "/images/services/phone-screen.svg"),
        new(
            "Ép kính, thay mặt kính điện thoại",
            "Ép mặt kính mới bằng công nghệ chân không chuẩn phòng sạch khi màn hình hiển thị và cảm ứng còn tốt.",
            500000m,
            "Phone",
            "/images/services/phone-glass.svg"),
        new(
            "Sửa lỗi chân sạc điện thoại",
            "Khắc phục tình trạng sạc không vào, sạc chập chờn, lỏng cáp Type-C/Lightning hoặc lỗi IC sạc.",
            300000m,
            "Phone",
            "/images/services/phone-charging.svg"),
        new(
            "Vệ sinh điện thoại & phục hồi kháng nước",
            "Làm sạch loa thoại, cổng sạc, cụm camera và dán lại ron áp suất chống bụi ẩm tiêu chuẩn Fixora.",
            150000m,
            "Phone",
            "/images/services/phone-cleaning.svg"),

        // Laptop (6 services)
        new(
            "Vệ sinh máy, tra keo tản nhiệt laptop",
            "Làm sạch hệ thống quạt tản nhiệt, khe gió và tra keo tản nhiệt hiệu suất cao giúp giảm nhiệt độ 8–15°C.",
            250000m,
            "Laptop",
            "/images/services/laptop-cleaning.svg"),
        new(
            "Thay bàn phím laptop",
            "Thay bàn phím laptop Dell, ASUS, Lenovo, HP, MacBook bị liệt phím, chập phím hoặc đổ chất lỏng.",
            450000m,
            "Laptop",
            "/images/services/laptop-keyboard.svg"),
        new(
            "Thay pin laptop",
            "Thay pin cell chuẩn cho MacBook và laptop Windows bị báo bảo trì, chai pin, sập nguồn đột ngột.",
            900000m,
            "Laptop",
            "/images/services/laptop-battery.svg"),
        new(
            "Thay màn hình laptop",
            "Thay tấm nền IPS/OLED FHD, 2K, 120Hz–165Hz chính hãng cho laptop bị nứt vỡ, đốm sáng hoặc sọc ngang.",
            1450000m,
            "Laptop",
            "/images/services/laptop-screen.svg"),
        new(
            "Nâng cấp SSD NVMe & RAM laptop",
            "Nâng cấp ổ cứng SSD PCIe Gen 4 và bộ nhớ RAM tốc độ cao, hỗ trợ sao lưu dữ liệu và tối ưu hệ điều hành.",
            850000m,
            "Laptop",
            "/images/services/laptop-ssd.svg"),
        new(
            "Sửa lỗi nguồn mainboard laptop",
            "Chẩn đoán và sửa chữa mạch nguồn, IC sạc, lỗi không kích được nguồn hoặc tự ngắt trên bo mạch laptop.",
            650000m,
            "Laptop",
            "/images/services/laptop-power.svg"),

        // Tablet (4 services)
        new(
            "Thay màn hình máy tính bảng",
            "Thay màn hình nguyên bộ cho iPad, Galaxy Tab, Xiaomi Pad đảm bảo tương thích bút cảm ứng và độ nhạy cao.",
            1200000m,
            "Tablet",
            "/images/services/tablet-screen.svg"),
        new(
            "Thay pin máy tính bảng",
            "Thay pin dung lượng lớn cho iPad và máy tính bảng Android bị phồng pin, tụt phần trăm nhanh.",
            650000m,
            "Tablet",
            "/images/services/tablet-battery.svg"),
        new(
            "Ép kính cảm ứng máy tính bảng",
            "Thay mặt kính cảm ứng iPad, Galaxy Tab bị rạn nứt giúp tiết kiệm chi phí mà vẫn giữ màn hình zin.",
            750000m,
            "Tablet",
            "/images/services/tablet-glass.svg"),
        new(
            "Sửa lỗi sạc máy tính bảng",
            "Thay cụm chân sạc USB-C / Lightning hoặc xử lý lỗi không nhận sạc nhanh trên các dòng tablet.",
            400000m,
            "Tablet",
            "/images/services/tablet-charging.svg"),

        // Smartwatch (3 services)
        new(
            "Thay pin đồng hồ thông minh",
            "Thay pin chuẩn kích thước cho Apple Watch, Galaxy Watch, Garmin kèm gia cố keo chống nước chuyên dụng.",
            450000m,
            "Smartwatch",
            "/images/services/watch-battery.svg"),
        new(
            "Thay màn hình smartwatch",
            "Thay màn hình hoặc ép kính cảm ứng cho đồng hồ thông minh bị va đập, mất hiển thị hoặc nứt mặt kính.",
            950000m,
            "Smartwatch",
            "/images/services/watch-screen.svg"),
        new(
            "Sửa lỗi cảm ứng & nút Digital Crown",
            "Khắc phục lỗi liệt cảm ứng, kẹt nút xoay Digital Crown, lỗi rung phản hồi hoặc cảm biến nhịp tim.",
            500000m,
            "Smartwatch",
            "/images/services/watch-touch.svg"),

        // Electronics (4 services)
        new(
            "Kiểm tra và vệ sinh mainboard",
            "Kiểm tra, vệ sinh siêu âm và sấy khô bo mạch cho thiết bị điện tử bị vào nước, ẩm mốc hoặc chập nhẹ.",
            200000m,
            "Electronics",
            "/images/services/electronics-mainboard.svg"),
        new(
            "Sửa tay cầm chơi game PS5 / Switch",
            "Thay analog Hall Effect chống trôi (drift), sửa nút bấm L/R, cò adaptive trigger và thay pin tay cầm.",
            280000m,
            "Electronics",
            "/images/services/electronics-controller.svg"),
        new(
            "Sửa tai nghe chống ồn cao cấp",
            "Thay pin, sửa bản lề gãy, khắc phục lỗi rè một bên hoặc lỗi chống ồn ANC trên tai nghe Sony, Marshall.",
            350000m,
            "Electronics",
            "/images/services/electronics-headphone.svg"),
        new(
            "Sửa loa Bluetooth & thiết bị âm thanh",
            "Sửa mạch công suất, thay gân loa, thay pin và khắc phục lỗi không lên nguồn trên loa Marshall, LG, Sony.",
            450000m,
            "Electronics",
            "/images/services/electronics-speaker.svg"),

        // General
        new(
            "Cài đặt phần mềm, xử lý virus",
            "Cài lại hệ điều hành, phần mềm cần thiết, quét và diệt virus. Áp dụng cho mọi loại thiết bị.",
            150000m,
            null,
            "/images/services/software-install.svg")
    ];
}
