using RepairShop.Domain.Modules.Inventory;

public interface IPartRepository
{
    Task<Part?> GetByIdAsync(Guid id);
    Task<List<Part>> ListAsync(string? search);
}

public interface IInventoryRepository
{
    Task<Inventory?> GetByPartIdAsync(Guid partId);
}