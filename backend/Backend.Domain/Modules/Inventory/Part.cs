using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Exceptions;

namespace RepairShop.Domain.Modules.Inventory;

public class Part : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string Sku { get; private set; } = default!;
    public string? Category { get; private set; }
    public string? CompatibleDeviceType { get; private set; }
    public decimal CostPrice { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string Unit { get; private set; } = "cái";
    public int MinStockThreshold { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Part() { } // for EF Core

    public Part(string name, string sku, decimal costPrice, decimal unitPrice,
        string? category = null, string? compatibleDeviceType = null,
        string unit = "cái", int minStockThreshold = 0)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Tên linh kiện không được để trống.");
        if (string.IsNullOrWhiteSpace(sku)) throw new DomainException("Mã SKU không được để trống.");
        if (costPrice < 0) throw new DomainException("Giá nhập không thể âm.");
        if (unitPrice < 0) throw new DomainException("Giá bán không thể âm.");
        if (minStockThreshold < 0) throw new DomainException("Ngưỡng tồn kho tối thiểu không thể âm.");


        Name = name;
        Sku = sku;
        UnitPrice = unitPrice;
        Unit = unit;
        MinStockThreshold = minStockThreshold;
    }

    public void UpdateInfo(string name, decimal costPrice, decimal unitPrice, string? category,
        string? compatibleDeviceType, string unit, int minStockThreshold)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Tên linh kiện không được để trống.");
        if (costPrice < 0) throw new DomainException("Giá nhập không thể âm.");
        if (unitPrice < 0) throw new DomainException("Giá bán không thể âm.");

        Name = name;
        CostPrice = costPrice;
        UnitPrice = unitPrice;
        Category = category;
        CompatibleDeviceType = compatibleDeviceType;
        Unit = unit;
        MinStockThreshold = minStockThreshold;
        MarkUpdated();
    }

    public void Deactivate() { IsActive = false; MarkUpdated(); }
    public void Activate() { IsActive = true; MarkUpdated(); }
}