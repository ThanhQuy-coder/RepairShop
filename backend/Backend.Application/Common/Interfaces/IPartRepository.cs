using RepairShop.Domain.Modules.Inventory;

public interface IPartRepository
{
    Task<Part?> GetByIdAsync(Guid id);
    Task<Part?> GetBySkuAsync(string sku);
    Task<(List<Part> Items, int Total)> SearchAsync(string? search, string? category, int page, int pageSize);
    Task<List<Part>> ListAsync(string? search);
    Task AddAsync(Part part);
    void Update(Part part);
    Task SaveChangesAsync();
}