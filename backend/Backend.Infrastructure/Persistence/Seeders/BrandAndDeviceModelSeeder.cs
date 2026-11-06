using Microsoft.EntityFrameworkCore;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Modules.Devices;

namespace RepairShop.Infrastructure.Persistence.Seeders;

public class BrandAndDeviceModelSeeder : IDataSeeder
{
    private readonly AppDbContext _context;

    public BrandAndDeviceModelSeeder(AppDbContext context) => _context = context;

    public int Order => 10;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedBrandsAsync(cancellationToken);
        await SeedDeviceModelsAsync(cancellationToken);
    }

    private async Task SeedBrandsAsync(CancellationToken cancellationToken)
    {
        var existingCodes = (await _context.Brands
            .Select(b => b.Code)
            .ToListAsync(cancellationToken))
            .Select(c => c.ToUpperInvariant())
            .ToHashSet();

        var brandsToAdd = GetBrandSeedDefinitions()
            .Where(def => existingCodes.Add(def.Code.ToUpperInvariant()))
            .Select(def => new Brand(def.Name, def.Code, def.LogoUrl, def.Description, true))
            .ToList();

        if (brandsToAdd.Count > 0)
        {
            await _context.Brands.AddRangeAsync(brandsToAdd, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SeedDeviceModelsAsync(CancellationToken cancellationToken)
    {
        var brandsByCode = await _context.Brands
            .Where(b => b.DeletedAt == null)
            .ToDictionaryAsync(b => b.Code.ToUpperInvariant(), b => b.Id, cancellationToken);

        var existingKeys = (await _context.DeviceModels
            .Select(m => new { m.BrandId, m.DeviceType, m.ModelName })
            .ToListAsync(cancellationToken))
            .Select(x => $"{x.BrandId}:{x.DeviceType}:{x.ModelName.ToLowerInvariant()}")
            .ToHashSet();

        var modelsToAdd = new List<DeviceModel>();

        foreach (var def in GetModelSeedDefinitions())
        {
            if (!brandsByCode.TryGetValue(def.BrandCode.ToUpperInvariant(), out var brandId))
                continue;

            var key = $"{brandId}:{def.DeviceType}:{def.ModelName.ToLowerInvariant()}";
            if (!existingKeys.Add(key))
                continue;

            modelsToAdd.Add(new DeviceModel(
                brandId,
                def.DeviceType,
                def.ModelName,
                def.ModelNumber,
                def.ReleaseYear,
                true));
        }

        if (modelsToAdd.Count > 0)
        {
            await _context.DeviceModels.AddRangeAsync(modelsToAdd, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed record BrandSeedDef(string Code, string Name, string LogoUrl, string Description);

    private sealed record ModelSeedDef(
        string BrandCode,
        DeviceType DeviceType,
        string ModelName,
        string? ModelNumber,
        int? ReleaseYear);

    private static IReadOnlyList<BrandSeedDef> GetBrandSeedDefinitions() =>
    [
        new("APPLE", "Apple", "/images/brands/apple.svg", "Hệ sinh thái thiết bị iPhone, MacBook, iPad và Apple Watch chính hãng."),
        new("SAMSUNG", "Samsung", "/images/brands/samsung.svg", "Điện thoại Galaxy, máy tính bảng Galaxy Tab và đồng hồ Galaxy Watch."),
        new("XIAOMI", "Xiaomi", "/images/brands/xiaomi.svg", "Smartphone Xiaomi, dòng Redmi Note và máy tính bảng Xiaomi Pad."),
        new("DELL", "Dell", "/images/brands/dell.svg", "Laptop doanh nhân Dell XPS, Inspiron, Latitude và máy trạm đồ họa."),
        new("ASUS", "ASUS", "/images/brands/asus.svg", "Laptop mỏng nhẹ Zenbook, Vivobook và laptop gaming ROG, TUF."),
        new("LENOVO", "Lenovo", "/images/brands/lenovo.svg", "Laptop doanh nhân ThinkPad, IdeaPad và laptop gaming Lenovo Legion."),
        new("HP", "HP", "/images/brands/hp.svg", "Laptop văn phòng và cao cấp HP Pavilion, Envy, Spectre và EliteBook."),
        new("GARMIN", "Garmin", "/images/brands/garmin.svg", "Đồng hồ thông minh thể thao và định vị GPS chuyên dụng."),
        new("SONY", "Sony", "/images/brands/sony.svg", "Máy chơi game PlayStation, tai nghe chống ồn WH-1000XM và thiết bị âm thanh."),
        new("MARSHALL", "Marshall", "/images/brands/marshall.svg", "Loa Bluetooth và tai nghe âm thanh cổ điển thương hiệu Anh Quốc."),
        new("NINTENDO", "Nintendo", "/images/brands/nintendo.svg", "Máy chơi game cầm tay Nintendo Switch và tay cầm Joy-Con."),
        new("LG", "LG", "/images/brands/lg.svg", "Màn hình máy tính UltraGear, loa di động XBOOM và thiết bị điện tử tiêu dùng.")
    ];

    private static IReadOnlyList<ModelSeedDef> GetModelSeedDefinitions() =>
    [
        // 1. Phone (9 models)
        new("APPLE", DeviceType.Phone, "iPhone 13", "A2633", 2021),
        new("APPLE", DeviceType.Phone, "iPhone 14 Pro", "A2890", 2022),
        new("APPLE", DeviceType.Phone, "iPhone 15 Pro Max", "A3106", 2023),
        new("SAMSUNG", DeviceType.Phone, "Samsung Galaxy S23 Ultra", "SM-S918B", 2023),
        new("SAMSUNG", DeviceType.Phone, "Samsung Galaxy S24 Ultra", "SM-S928B", 2024),
        new("SAMSUNG", DeviceType.Phone, "Samsung Galaxy A54 5G", "SM-A546E", 2023),
        new("XIAOMI", DeviceType.Phone, "Xiaomi Redmi Note 13", "23129RAA4G", 2024),
        new("XIAOMI", DeviceType.Phone, "Xiaomi 14", "23127PN0CG", 2024),
        new("XIAOMI", DeviceType.Phone, "Xiaomi 13T Pro", "23078PND5G", 2023),

        // 2. Laptop (10 models)
        new("APPLE", DeviceType.Laptop, "MacBook Air M2", "A2681", 2022),
        new("APPLE", DeviceType.Laptop, "MacBook Pro M3 Pro", "A2992", 2023),
        new("DELL", DeviceType.Laptop, "Dell XPS 13 9315", "9315", 2022),
        new("DELL", DeviceType.Laptop, "Dell Inspiron 3520", "N3520", 2022),
        new("ASUS", DeviceType.Laptop, "ASUS ROG Strix G16", "G614JU", 2023),
        new("ASUS", DeviceType.Laptop, "ASUS Zenbook 14 OLED", "UX3405MA", 2024),
        new("LENOVO", DeviceType.Laptop, "Lenovo ThinkPad T14 Gen 4", "21HD", 2023),
        new("LENOVO", DeviceType.Laptop, "Lenovo Legion 5", "15IAH7H", 2022),
        new("HP", DeviceType.Laptop, "HP Pavilion 15", "15-eg2081TU", 2022),
        new("HP", DeviceType.Laptop, "HP Envy x360 14", "14-es0013dx", 2023),

        // 3. Tablet (7 models)
        new("APPLE", DeviceType.Tablet, "iPad 9", "A2602", 2021),
        new("APPLE", DeviceType.Tablet, "iPad Air 5", "A2588", 2022),
        new("APPLE", DeviceType.Tablet, "iPad Pro 11 M2", "A2759", 2022),
        new("SAMSUNG", DeviceType.Tablet, "Galaxy Tab S9 FE", "SM-X510", 2023),
        new("SAMSUNG", DeviceType.Tablet, "Galaxy Tab A9", "SM-X110", 2023),
        new("SAMSUNG", DeviceType.Tablet, "Galaxy Tab S9 Ultra", "SM-X910", 2023),
        new("XIAOMI", DeviceType.Tablet, "Xiaomi Pad 6", "23043RP34G", 2023),

        // 4. Smartwatch (6 models)
        new("APPLE", DeviceType.Smartwatch, "Apple Watch Series 9", "A2980", 2023),
        new("APPLE", DeviceType.Smartwatch, "Apple Watch Ultra 2", "A2986", 2023),
        new("SAMSUNG", DeviceType.Smartwatch, "Galaxy Watch 6 Classic", "SM-R960", 2023),
        new("SAMSUNG", DeviceType.Smartwatch, "Galaxy Watch 5 Pro", "SM-R920", 2022),
        new("GARMIN", DeviceType.Smartwatch, "Garmin Forerunner 265", "010-02810", 2023),
        new("GARMIN", DeviceType.Smartwatch, "Garmin Fenix 7 Pro", "010-02777", 2023),

        // 5. Electronics (8 models)
        new("SONY", DeviceType.Electronics, "PlayStation 5", "CFI-2000", 2023),
        new("SONY", DeviceType.Electronics, "Sony WH-1000XM5", "YY2954", 2022),
        new("SONY", DeviceType.Electronics, "Tay cầm DualSense PS5", "CFI-ZCT1W", 2020),
        new("NINTENDO", DeviceType.Electronics, "Nintendo Switch OLED", "HEG-001", 2021),
        new("MARSHALL", DeviceType.Electronics, "Marshall Emberton II", "1006234", 2022),
        new("MARSHALL", DeviceType.Electronics, "Marshall Stanmore III", "1006014", 2022),
        new("LG", DeviceType.Electronics, "Loa LG XBOOM Go XG7Q", "XG7QBK", 2022),
        new("LG", DeviceType.Electronics, "Màn hình LG UltraGear 27GP850", "27GP850-B", 2021)
    ];
}
