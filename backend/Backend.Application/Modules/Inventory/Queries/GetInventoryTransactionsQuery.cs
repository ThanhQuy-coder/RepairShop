using RepairShop.Application.Common.Interfaces;
using MediatR;

namespace RepairShop.Application.Modules.Inventory.Queries;

public record TransactionListResponse(List<InventoryTransactionView> Items, int Total);

public record GetInventoryTransactionsQuery(
    Guid? PartId, string? Type, DateTime? FromDate, DateTime? ToDate, int Page = 1, int PageSize = 20)
    : IRequest<TransactionListResponse>;

public class GetInventoryTransactionsQueryHandler : IRequestHandler<GetInventoryTransactionsQuery, TransactionListResponse>
{
    private readonly IInventoryRepository _inventoryRepository;
    public GetInventoryTransactionsQueryHandler(IInventoryRepository inventoryRepository) =>
        _inventoryRepository = inventoryRepository;

    public async Task<TransactionListResponse> Handle(GetInventoryTransactionsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _inventoryRepository.SearchTransactionsAsync(
            request.PartId, request.Type, request.FromDate, request.ToDate, request.Page, request.PageSize);

        return new TransactionListResponse(items, total);
    }
}