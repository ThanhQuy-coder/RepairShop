using MediatR;

namespace RepairShop.Application.Modules.Inventory.Queries;

public record PartResponse(Guid Id, string Name,
    string Sku, decimal UnitPrice, string Unit);
public record GetPartsQuery(string? Search) : IRequest<List<PartResponse>>;

public class GetPartsQueryHandler : IRequestHandler<GetPartsQuery, List<PartResponse>>
{
    private readonly IPartRepository _partRepository;
    public GetPartsQueryHandler(IPartRepository partRepository) =>
        _partRepository = partRepository;

    public async Task<List<PartResponse>> Handle(GetPartsQuery request,
        CancellationToken cancellationToken)
    {
        var parts = await _partRepository.ListAsync(request.Search);
        return parts.Select(p => new PartResponse(p.Id, p.Name, p.Sku,
            p.UnitPrice, p.Unit)).ToList();
    }
}