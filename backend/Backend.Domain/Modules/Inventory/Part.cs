using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Exceptions;

namespace RepairShop.Domain.Modules.Inventory;

public class Part : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string Sku { get; private set; } = default!;
    public decimal UnitPrice { get; private set; }
    public string Unit { get; private set; } = "cái";
    public int MinStockThreshold { get; private set; }

    private Part() { } // for EF Core

    public Part(string name, string sku, decimal unitPrice, string unit = "cái", int minStockThreshold = 0)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Tên linh kiện không được để trống.");
        if (unitPrice < 0) throw new DomainException("Đơn giá linh kiện không thể âm.");

        Name = name;
        Sku = sku;
        UnitPrice = unitPrice;
        Unit = unit;
        MinStockThreshold = minStockThreshold;
    }
}