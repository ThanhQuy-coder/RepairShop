
using Microsoft.EntityFrameworkCore;
using RepairShop.Domain.Modules.Inventory;

namespace RepairShop.Infrastructure.Persistence.Seeders;

public class PartSeeder : IDataSeeder
{
    private readonly AppDbContext _context;

    public PartSeeder(AppDbContext context)
    {
        _context = context;
    }

    public int Order => 20;

    public async Task SeedAsync(
    CancellationToken cancellationToken = default)
    {
        var existingSkus = await _context.Set<Part>()
            .Select(p => p.Sku)
            .ToListAsync(cancellationToken);

        var existingSkuSet = existingSkus.ToHashSet(
            StringComparer.OrdinalIgnoreCase);

        var parts = GetSeedParts()
            .Where(p => !existingSkuSet.Contains(p.Sku))
            .ToList();

        if (parts.Count == 0)
            return;

        await _context.Set<Part>().AddRangeAsync(
            parts,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static List<Part> GetSeedParts()
    {
        return
        [
            // =========================
            // PHONE - Màn hình
            // =========================
            new Part(
            name: "Màn hình iPhone 11",
            sku: "SCR-IP11-001",
            costPrice: 850000m,
            unitPrice: 1200000m,
            category: "Màn hình",
            compatibleDeviceType: "Phone",
            unit: "cái",
            minStockThreshold: 5
        ),
        new Part(
            name: "Màn hình iPhone 13",
            sku: "SCR-IP13-001",
            costPrice: 1450000m,
            unitPrice: 1950000m,
            category: "Màn hình",
            compatibleDeviceType: "Phone",
            unit: "cái"
        ),
        new Part(
            name: "Màn hình Samsung Galaxy A54",
            sku: "SCR-SA54-001",
            costPrice: 1100000m,
            unitPrice: 1550000m,
            category: "Màn hình",
            compatibleDeviceType: "Phone",
            unit: "cái"
        ),

        // =========================
        // PHONE - Pin
        // =========================
        new Part(
            name: "Pin iPhone 11",
            sku: "BAT-IP11-001",
            costPrice: 250000m,
            unitPrice: 450000m,
            category: "Pin",
            compatibleDeviceType: "Phone",
            unit: "cái"
        ),
        new Part(
            name: "Pin iPhone 13",
            sku: "BAT-IP13-001",
            costPrice: 380000m,
            unitPrice: 650000m,
            category: "Pin",
            compatibleDeviceType: "Phone",
            unit: "cái"
        ),
        new Part(
            name: "Pin Samsung Galaxy A54",
            sku: "BAT-SA54-001",
            costPrice: 300000m,
            unitPrice: 520000m,
            category: "Pin",
            compatibleDeviceType: "Phone",
            unit: "cái"
        ),

        // =========================
        // PHONE - Linh kiện khác
        // =========================
        new Part(
            name: "Cụm chân sạc iPhone 11",
            sku: "CHG-IP11-001",
            costPrice: 180000m,
            unitPrice: 350000m,
            category: "Chân sạc",
            compatibleDeviceType: "Phone",
            unit: "cái"
        ),
        new Part(
            name: "Camera sau iPhone 13",
            sku: "CAM-IP13-001",
            costPrice: 650000m,
            unitPrice: 950000m,
            category: "Camera",
            compatibleDeviceType: "Phone",
            unit: "cái"
        ),
        new Part(
            name: "Loa trong Samsung Galaxy A54",
            sku: "SPK-SA54-001",
            costPrice: 120000m,
            unitPrice: 250000m,
            category: "Loa",
            compatibleDeviceType: "Phone",
            unit: "cái"
        ),

        // =========================
        // LAPTOP - RAM, SSD
        // =========================
        new Part(
            name: "RAM DDR4 Laptop 8GB",
            sku: "RAM-DDR4-8GB-001",
            costPrice: 350000m,
            unitPrice: 500000m,
            category: "RAM",
            compatibleDeviceType: "Laptop",
            unit: "thanh"
        ),
        new Part(
            name: "RAM DDR4 Laptop 16GB",
            sku: "RAM-DDR4-16GB-001",
            costPrice: 650000m,
            unitPrice: 850000m,
            category: "RAM",
            compatibleDeviceType: "Laptop",
            unit: "thanh"
        ),
        new Part(
            name: "SSD SATA 2.5 inch 256GB",
            sku: "SSD-SATA-256GB-001",
            costPrice: 550000m,
            unitPrice: 750000m,
            category: "Ổ cứng",
            compatibleDeviceType: "Laptop",
            unit: "cái"
        ),
        new Part(
            name: "SSD NVMe M.2 512GB",
            sku: "SSD-NVME-512GB-001",
            costPrice: 850000m,
            unitPrice: 1100000m,
            category: "Ổ cứng",
            compatibleDeviceType: "Laptop",
            unit: "cái"
        ),

        // =========================
        // LAPTOP - Màn hình, pin, sạc
        // =========================
        new Part(
            name: "Màn hình Laptop 15.6 inch FHD",
            sku: "SCR-LAP-156FHD-001",
            costPrice: 1250000m,
            unitPrice: 1650000m,
            category: "Màn hình",
            compatibleDeviceType: "Laptop",
            unit: "cái"
        ),
        new Part(
            name: "Pin Laptop Dell Inspiron",
            sku: "BAT-DELL-INS-001",
            costPrice: 650000m,
            unitPrice: 950000m,
            category: "Pin",
            compatibleDeviceType: "Laptop",
            unit: "cái"
        ),
        new Part(
            name: "Sạc Laptop USB-C 65W",
            sku: "ADP-USBC-65W-001",
            costPrice: 350000m,
            unitPrice: 550000m,
            category: "Sạc",
            compatibleDeviceType: "Laptop",
            unit: "cái"
        ),
        new Part(
            name: "Quạt tản nhiệt Laptop Dell",
            sku: "FAN-DELL-LAP-001",
            costPrice: 220000m,
            unitPrice: 380000m,
            category: "Tản nhiệt",
            compatibleDeviceType: "Laptop",
            unit: "cái"
        ),

        // =========================
        // ELECTRONICS - Linh kiện chung
        // =========================
        new Part(
            name: "Nguồn Adapter 12V 2A",
            sku: "PWR-12V-2A-001",
            costPrice: 80000m,
            unitPrice: 150000m,
            category: "Nguồn điện",
            compatibleDeviceType: "Electronics",
            unit: "cái"
        ),
        new Part(
            name: "Cáp kết nối USB-C",
            sku: "CBL-USBC-001",
            costPrice: 40000m,
            unitPrice: 90000m,
            category: "Cáp kết nối",
            compatibleDeviceType: "Electronics",
            unit: "sợi"
        ),
        new Part(
            name: "Bo mạch điều khiển thiết bị",
            sku: "PCB-CTRL-001",
            costPrice: 250000m,
            unitPrice: 450000m,
            category: "Bo mạch",
            compatibleDeviceType: "Electronics",
            unit: "cái"
        )
        ];
    }
}
