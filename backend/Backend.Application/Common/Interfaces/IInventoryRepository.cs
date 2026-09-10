using RepairShop.Domain.Modules.Inventory;

namespace RepairShop.Application.Common.Interfaces;

public interface IInventoryRepository
{
    Task<Inventory?> GetByPartIdAsync(Guid partId);
    Task AddAsync(Inventory inventory);
    void TrackNewTransaction(InventoryTransaction transaction); // đúng pattern TrackNewX() đã học (Task 4.5+)
    Task<(List<InventoryTransactionView> Items, int Total)> SearchTransactionsAsync(
        Guid? partId, string? type, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task SaveChangesAsync();
}

public record InventoryTransactionView(
    Guid Id, Guid PartId, string PartName, string Type, int Quantity,
    Guid? RelatedTicketId, string PerformedByName, DateTime CreatedAt);