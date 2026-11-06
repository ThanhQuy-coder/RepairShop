using FluentValidation;
using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Devices.DTOs;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Common.Exceptions;
using RepairShop.Domain.Modules.Devices;

namespace RepairShop.Application.Modules.Devices;

public record GetBrandsQuery(
    string? DeviceType = null,
    bool? IsActive = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 100) : IRequest<BrandListResponse>;

public record GetBrandByIdQuery(Guid Id) : IRequest<BrandResponse>;

public record CreateBrandCommand(
    string Name,
    string Code,
    string? LogoUrl = null,
    string? Description = null,
    bool IsActive = true) : IRequest<BrandResponse>;

public record UpdateBrandCommand(
    Guid Id,
    string Name,
    string Code,
    string? LogoUrl = null,
    string? Description = null,
    bool? IsActive = null) : IRequest<BrandResponse>;

public record ToggleBrandActiveCommand(Guid Id, bool IsActive) : IRequest<BrandResponse>;

public record DeleteBrandCommand(Guid Id) : IRequest<Unit>;

public class CreateBrandCommandValidator : AbstractValidator<CreateBrandCommand>
{
    public CreateBrandCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên hãng sản xuất không được để trống.")
            .MaximumLength(100).WithMessage("Tên hãng sản xuất tối đa 100 ký tự.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã hãng sản xuất không được để trống.")
            .MaximumLength(50).WithMessage("Mã hãng sản xuất tối đa 50 ký tự.")
            .Matches("^[A-Za-z0-9_-]+$").WithMessage("Mã hãng sản xuất chỉ chứa chữ cái, chữ số, dấu gạch ngang hoặc gạch dưới.");

        RuleFor(x => x.LogoUrl)
            .MaximumLength(500).WithMessage("Đường dẫn logo tối đa 500 ký tự.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Mô tả tối đa 500 ký tự.");
    }
}

public class UpdateBrandCommandValidator : AbstractValidator<UpdateBrandCommand>
{
    public UpdateBrandCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên hãng sản xuất không được để trống.")
            .MaximumLength(100).WithMessage("Tên hãng sản xuất tối đa 100 ký tự.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã hãng sản xuất không được để trống.")
            .MaximumLength(50).WithMessage("Mã hãng sản xuất tối đa 50 ký tự.")
            .Matches("^[A-Za-z0-9_-]+$").WithMessage("Mã hãng sản xuất chỉ chứa chữ cái, chữ số, dấu gạch ngang hoặc gạch dưới.");

        RuleFor(x => x.LogoUrl)
            .MaximumLength(500).WithMessage("Đường dẫn logo tối đa 500 ký tự.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Mô tả tối đa 500 ký tự.");
    }
}

public class BrandHandlers :
    IRequestHandler<GetBrandsQuery, BrandListResponse>,
    IRequestHandler<GetBrandByIdQuery, BrandResponse>,
    IRequestHandler<CreateBrandCommand, BrandResponse>,
    IRequestHandler<UpdateBrandCommand, BrandResponse>,
    IRequestHandler<ToggleBrandActiveCommand, BrandResponse>,
    IRequestHandler<DeleteBrandCommand, Unit>
{
    private readonly IBrandRepository _brandRepository;

    public BrandHandlers(IBrandRepository brandRepository) => _brandRepository = brandRepository;

    public async Task<BrandListResponse> Handle(GetBrandsQuery request, CancellationToken cancellationToken)
    {
        DeviceType? parsedDeviceType = null;
        if (!string.IsNullOrWhiteSpace(request.DeviceType))
        {
            if (!Enum.TryParse<DeviceType>(request.DeviceType.Trim(), true, out var dt))
            {
                throw new DomainException($"Loại thiết bị '{request.DeviceType}' không hợp lệ.");
            }
            parsedDeviceType = dt;
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var (items, total, deviceTypesByBrand, modelCountByBrand) = await _brandRepository.SearchAsync(
            parsedDeviceType,
            request.IsActive,
            request.Search,
            page,
            pageSize);

        var responses = items.Select(b => MapToResponse(
            b,
            deviceTypesByBrand.GetValueOrDefault(b.Id) ?? new List<string>(),
            modelCountByBrand.GetValueOrDefault(b.Id, 0))).ToList();

        return new BrandListResponse(responses, total);
    }

    public async Task<BrandResponse> Handle(GetBrandByIdQuery request, CancellationToken cancellationToken)
    {
        var brand = await _brandRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Hãng sản xuất", request.Id);

        var (_, _, deviceTypesByBrand, modelCountByBrand) = await _brandRepository.SearchAsync(
            null, null, brand.Code, 1, 20);

        return MapToResponse(
            brand,
            deviceTypesByBrand.GetValueOrDefault(brand.Id) ?? new List<string>(),
            modelCountByBrand.GetValueOrDefault(brand.Id, 0));
    }

    public async Task<BrandResponse> Handle(CreateBrandCommand request, CancellationToken cancellationToken)
    {
        if (await _brandRepository.ExistsByCodeAsync(request.Code))
        {
            throw new DomainException($"Mã hãng sản xuất '{request.Code.Trim().ToUpperInvariant()}' đã tồn tại.");
        }

        if (await _brandRepository.ExistsByNameAsync(request.Name))
        {
            throw new DomainException($"Tên hãng sản xuất '{request.Name.Trim()}' đã tồn tại.");
        }

        var brand = new Brand(request.Name, request.Code, request.LogoUrl, request.Description, request.IsActive);
        await _brandRepository.AddAsync(brand);
        await _brandRepository.SaveChangesAsync();

        return MapToResponse(brand, new List<string>(), 0);
    }

    public async Task<BrandResponse> Handle(UpdateBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = await _brandRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Hãng sản xuất", request.Id);

        if (await _brandRepository.ExistsByCodeAsync(request.Code, request.Id))
        {
            throw new DomainException($"Mã hãng sản xuất '{request.Code.Trim().ToUpperInvariant()}' đã tồn tại.");
        }

        if (await _brandRepository.ExistsByNameAsync(request.Name, request.Id))
        {
            throw new DomainException($"Tên hãng sản xuất '{request.Name.Trim()}' đã tồn tại.");
        }

        brand.UpdateInfo(request.Name, request.Code, request.LogoUrl, request.Description);
        if (request.IsActive.HasValue)
        {
            brand.SetActive(request.IsActive.Value);
        }

        _brandRepository.Update(brand);
        await _brandRepository.SaveChangesAsync();

        var (_, _, deviceTypesByBrand, modelCountByBrand) = await _brandRepository.SearchAsync(
            null, null, brand.Code, 1, 20);

        return MapToResponse(
            brand,
            deviceTypesByBrand.GetValueOrDefault(brand.Id) ?? new List<string>(),
            modelCountByBrand.GetValueOrDefault(brand.Id, 0));
    }

    public async Task<BrandResponse> Handle(ToggleBrandActiveCommand request, CancellationToken cancellationToken)
    {
        var brand = await _brandRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Hãng sản xuất", request.Id);

        brand.SetActive(request.IsActive);
        _brandRepository.Update(brand);
        await _brandRepository.SaveChangesAsync();

        var (_, _, deviceTypesByBrand, modelCountByBrand) = await _brandRepository.SearchAsync(
            null, null, brand.Code, 1, 20);

        return MapToResponse(
            brand,
            deviceTypesByBrand.GetValueOrDefault(brand.Id) ?? new List<string>(),
            modelCountByBrand.GetValueOrDefault(brand.Id, 0));
    }

    public async Task<Unit> Handle(DeleteBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = await _brandRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Hãng sản xuất", request.Id);

        if (await _brandRepository.IsReferencedAsync(request.Id))
        {
            throw new DomainException("Không thể xóa hãng sản xuất đang có dòng máy hoặc thiết bị tham chiếu. Vui lòng ngưng kích hoạt thay vì xóa.");
        }

        brand.MarkDeleted();
        _brandRepository.Update(brand);
        await _brandRepository.SaveChangesAsync();

        return Unit.Value;
    }

    private static BrandResponse MapToResponse(Brand brand, List<string> deviceTypes, int modelCount) =>
        new(
            brand.Id,
            brand.Name,
            brand.Code,
            brand.LogoUrl,
            brand.Description,
            brand.IsActive,
            deviceTypes,
            modelCount,
            brand.CreatedAt,
            brand.UpdatedAt);
}
