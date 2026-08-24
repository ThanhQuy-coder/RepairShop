using RepairShop.Domain.Modules.Inventory;

public interface IPartRepository
{
    Task<Part?> GetByIdAsync(Guid id);
}

public interface IInventoryRepository
{
    Task<Inventory?> GetByPartIdAsync(Guid partId);
}