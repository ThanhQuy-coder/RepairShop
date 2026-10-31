using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Warranty.DTOs;
using RepairShop.Domain.Modules.Warranty;
using RepairShop.Domain.Common.Exceptions;

namespace RepairShop.Application.Modules.WarrantyPackage;

public record GetWarrantyPackagesQuery(bool? IsActive) : IRequest<List<WarrantyPackageResponse>>;
public record CreateWarrantyPackageCommand(string Name, string? Description, int DurationMonths, string Terms)
    : IRequest<WarrantyPackageResponse>;
public record UpdateWarrantyPackageCommand(Guid Id, string Name, string? Description, int DurationMonths, string Terms)
    : IRequest<WarrantyPackageResponse>;
public record ToggleWarrantyPackageCommand(Guid Id, bool IsActive) : IRequest;
public record DeleteWarrantyPackageCommand(Guid Id) : IRequest;

public class GetWarrantyPackagesHandler(IWarrantyPackageRepository repository)
    : IRequestHandler<GetWarrantyPackagesQuery, List<WarrantyPackageResponse>>
{
    public async Task<List<WarrantyPackageResponse>> Handle(GetWarrantyPackagesQuery request, CancellationToken cancellationToken) =>
        (await repository.SearchAsync(request.IsActive)).Select(ToResponse).ToList();

    internal static WarrantyPackageResponse ToResponse(RepairShop.Domain.Modules.Warranty.WarrantyPackage p) =>
        new(p.Id, p.Name, p.Description, p.DurationMonths, p.Terms, p.IsActive);
}

public class CreateWarrantyPackageHandler(IWarrantyPackageRepository repository)
    : IRequestHandler<CreateWarrantyPackageCommand, WarrantyPackageResponse>
{
    public async Task<WarrantyPackageResponse> Handle(CreateWarrantyPackageCommand request, CancellationToken cancellationToken)
    {
        var package = new RepairShop.Domain.Modules.Warranty.WarrantyPackage(
            request.Name, request.Description, request.DurationMonths, request.Terms);
        await repository.AddAsync(package);
        await repository.SaveChangesAsync();
        return GetWarrantyPackagesHandler.ToResponse(package);
    }
}

public class UpdateWarrantyPackageHandler(IWarrantyPackageRepository repository)
    : IRequestHandler<UpdateWarrantyPackageCommand, WarrantyPackageResponse>
{
    public async Task<WarrantyPackageResponse> Handle(UpdateWarrantyPackageCommand request, CancellationToken cancellationToken)
    {
        var package = await repository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Gói bảo hành", request.Id);
        package.UpdateInfo(request.Name, request.Description, request.DurationMonths, request.Terms);
        repository.Update(package);
        await repository.SaveChangesAsync();
        return GetWarrantyPackagesHandler.ToResponse(package);
    }
}

public class ToggleWarrantyPackageHandler(IWarrantyPackageRepository repository)
    : IRequestHandler<ToggleWarrantyPackageCommand>
{
    public async Task Handle(ToggleWarrantyPackageCommand request, CancellationToken cancellationToken)
    {
        var package = await repository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Gói bảo hành", request.Id);
        package.SetActive(request.IsActive);
        repository.Update(package);
        await repository.SaveChangesAsync();
    }
}

public class DeleteWarrantyPackageHandler(IWarrantyPackageRepository repository)
    : IRequestHandler<DeleteWarrantyPackageCommand>
{
    public async Task Handle(DeleteWarrantyPackageCommand request, CancellationToken cancellationToken)
    {
        var package = await repository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Gói bảo hành", request.Id);
        if (await repository.IsUsedAsync(request.Id))
            throw new DomainException("Không thể xóa gói bảo hành đã được sử dụng.");
        package.MarkDeleted();
        repository.Update(package);
        await repository.SaveChangesAsync();
    }
}
