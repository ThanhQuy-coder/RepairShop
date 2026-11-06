using FluentValidation;
using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Devices.DTOs;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Common.Exceptions;
using RepairShop.Domain.Modules.Devices;

namespace RepairShop.Application.Modules.Devices;

public record GetDeviceModelsQuery(
    string? DeviceType = null,
    Guid? BrandId = null,
    bool? IsActive = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 100) : IRequest<DeviceModelListResponse>;

public record GetDeviceModelByIdQuery(Guid Id) : IRequest<DeviceModelResponse>;

public record CreateDeviceModelCommand(
    Guid BrandId,
    string DeviceType,
    string ModelName,
    string? ModelNumber = null,
    int? ReleaseYear = null,
    bool IsActive = true) : IRequest<DeviceModelResponse>;

public record UpdateDeviceModelCommand(
    Guid Id,
    Guid BrandId,
    string DeviceType,
    string ModelName,
    string? ModelNumber = null,
    int? ReleaseYear = null,
    bool? IsActive = null) : IRequest<DeviceModelResponse>;

public record ToggleDeviceModelActiveCommand(Guid Id, bool IsActive) : IRequest<DeviceModelResponse>;

public record DeleteDeviceModelCommand(Guid Id) : IRequest<Unit>;

public class CreateDeviceModelCommandValidator : AbstractValidator<CreateDeviceModelCommand>
{
    private static readonly string[] AllowedDeviceTypes = ["Phone", "Laptop", "Tablet", "Smartwatch", "Electronics"];

    public CreateDeviceModelCommandValidator()
    {
        RuleFor(x => x.BrandId)
            .NotEmpty().WithMessage("Hãng sản xuất không được để trống.");

        RuleFor(x => x.DeviceType)
            .NotEmpty().WithMessage("Loại thiết bị không được để trống.")
            .Must(t => AllowedDeviceTypes.Contains(t?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Loại thiết bị phải là Phone, Laptop, Tablet, Smartwatch hoặc Electronics.");

        RuleFor(x => x.ModelName)
            .NotEmpty().WithMessage("Tên dòng máy không được để trống.")
            .MaximumLength(150).WithMessage("Tên dòng máy tối đa 150 ký tự.");

        RuleFor(x => x.ModelNumber)
            .MaximumLength(100).WithMessage("Mã model kỹ thuật tối đa 100 ký tự.");

        RuleFor(x => x.ReleaseYear)
            .InclusiveBetween(1990, 2100)
            .When(x => x.ReleaseYear.HasValue)
            .WithMessage("Năm phát hành phải nằm trong khoảng 1990 đến 2100.");
    }
}

public class UpdateDeviceModelCommandValidator : AbstractValidator<UpdateDeviceModelCommand>
{
    private static readonly string[] AllowedDeviceTypes = ["Phone", "Laptop", "Tablet", "Smartwatch", "Electronics"];

    public UpdateDeviceModelCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.BrandId)
            .NotEmpty().WithMessage("Hãng sản xuất không được để trống.");

        RuleFor(x => x.DeviceType)
            .NotEmpty().WithMessage("Loại thiết bị không được để trống.")
            .Must(t => AllowedDeviceTypes.Contains(t?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Loại thiết bị phải là Phone, Laptop, Tablet, Smartwatch hoặc Electronics.");

        RuleFor(x => x.ModelName)
            .NotEmpty().WithMessage("Tên dòng máy không được để trống.")
            .MaximumLength(150).WithMessage("Tên dòng máy tối đa 150 ký tự.");

        RuleFor(x => x.ModelNumber)
            .MaximumLength(100).WithMessage("Mã model kỹ thuật tối đa 100 ký tự.");

        RuleFor(x => x.ReleaseYear)
            .InclusiveBetween(1990, 2100)
            .When(x => x.ReleaseYear.HasValue)
            .WithMessage("Năm phát hành phải nằm trong khoảng 1990 đến 2100.");
    }
}

public class DeviceModelHandlers :
    IRequestHandler<GetDeviceModelsQuery, DeviceModelListResponse>,
    IRequestHandler<GetDeviceModelByIdQuery, DeviceModelResponse>,
    IRequestHandler<CreateDeviceModelCommand, DeviceModelResponse>,
    IRequestHandler<UpdateDeviceModelCommand, DeviceModelResponse>,
    IRequestHandler<ToggleDeviceModelActiveCommand, DeviceModelResponse>,
    IRequestHandler<DeleteDeviceModelCommand, Unit>
{
    private readonly IDeviceModelRepository _deviceModelRepository;
    private readonly IBrandRepository _brandRepository;

    public DeviceModelHandlers(
        IDeviceModelRepository deviceModelRepository,
        IBrandRepository brandRepository)
    {
        _deviceModelRepository = deviceModelRepository;
        _brandRepository = brandRepository;
    }

    public async Task<DeviceModelListResponse> Handle(GetDeviceModelsQuery request, CancellationToken cancellationToken)
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

        var (items, total) = await _deviceModelRepository.SearchAsync(
            parsedDeviceType,
            request.BrandId,
            request.IsActive,
            request.Search,
            page,
            pageSize);

        var responses = items.Select(m => MapToResponse(m, m.Brand)).ToList();
        return new DeviceModelListResponse(responses, total);
    }

    public async Task<DeviceModelResponse> Handle(GetDeviceModelByIdQuery request, CancellationToken cancellationToken)
    {
        var model = await _deviceModelRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Dòng máy", request.Id);

        return MapToResponse(model, model.Brand);
    }

    public async Task<DeviceModelResponse> Handle(CreateDeviceModelCommand request, CancellationToken cancellationToken)
    {
        var brand = await _brandRepository.GetByIdAsync(request.BrandId)
            ?? throw new NotFoundException("Hãng sản xuất", request.BrandId);

        if (!brand.IsActive)
        {
            throw new DomainException("Hãng sản xuất đang ngưng hoạt động, không thể thêm dòng máy mới.");
        }

        if (!Enum.TryParse<DeviceType>(request.DeviceType.Trim(), true, out var deviceType))
        {
            throw new DomainException($"Loại thiết bị '{request.DeviceType}' không hợp lệ.");
        }

        if (await _deviceModelRepository.ExistsAsync(request.BrandId, deviceType, request.ModelName))
        {
            throw new DomainException($"Dòng máy '{request.ModelName.Trim()}' đã tồn tại cho hãng '{brand.Name}' và loại thiết bị '{deviceType}'.");
        }

        var model = new DeviceModel(
            request.BrandId,
            deviceType,
            request.ModelName,
            request.ModelNumber,
            request.ReleaseYear,
            request.IsActive);

        await _deviceModelRepository.AddAsync(model);
        await _deviceModelRepository.SaveChangesAsync();

        return MapToResponse(model, brand);
    }

    public async Task<DeviceModelResponse> Handle(UpdateDeviceModelCommand request, CancellationToken cancellationToken)
    {
        var model = await _deviceModelRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Dòng máy", request.Id);

        var brand = await _brandRepository.GetByIdAsync(request.BrandId)
            ?? throw new NotFoundException("Hãng sản xuất", request.BrandId);

        if (!brand.IsActive && (request.IsActive == true || request.BrandId != model.BrandId))
        {
            throw new DomainException("Hãng sản xuất đang ngưng hoạt động.");
        }

        if (!Enum.TryParse<DeviceType>(request.DeviceType.Trim(), true, out var deviceType))
        {
            throw new DomainException($"Loại thiết bị '{request.DeviceType}' không hợp lệ.");
        }

        if (await _deviceModelRepository.ExistsAsync(request.BrandId, deviceType, request.ModelName, request.Id))
        {
            throw new DomainException($"Dòng máy '{request.ModelName.Trim()}' đã tồn tại cho hãng '{brand.Name}' và loại thiết bị '{deviceType}'.");
        }

        model.UpdateInfo(request.BrandId, deviceType, request.ModelName, request.ModelNumber, request.ReleaseYear);
        if (request.IsActive.HasValue)
        {
            model.SetActive(request.IsActive.Value);
        }

        _deviceModelRepository.Update(model);
        await _deviceModelRepository.SaveChangesAsync();

        return MapToResponse(model, brand);
    }

    public async Task<DeviceModelResponse> Handle(ToggleDeviceModelActiveCommand request, CancellationToken cancellationToken)
    {
        var model = await _deviceModelRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Dòng máy", request.Id);

        if (request.IsActive && !model.Brand.IsActive)
        {
            throw new DomainException("Không thể kích hoạt dòng máy khi hãng sản xuất đang ngưng hoạt động.");
        }

        model.SetActive(request.IsActive);
        _deviceModelRepository.Update(model);
        await _deviceModelRepository.SaveChangesAsync();

        return MapToResponse(model, model.Brand);
    }

    public async Task<Unit> Handle(DeleteDeviceModelCommand request, CancellationToken cancellationToken)
    {
        var model = await _deviceModelRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Dòng máy", request.Id);

        if (await _deviceModelRepository.IsReferencedAsync(request.Id))
        {
            throw new DomainException("Không thể xóa dòng máy đang được thiết bị khách hàng tham chiếu. Vui lòng ngưng kích hoạt thay vì xóa.");
        }

        model.MarkDeleted();
        _deviceModelRepository.Update(model);
        await _deviceModelRepository.SaveChangesAsync();

        return Unit.Value;
    }

    private static DeviceModelResponse MapToResponse(DeviceModel model, Brand brand) =>
        new(
            model.Id,
            model.BrandId,
            brand.Name,
            brand.Code,
            model.DeviceType.ToString(),
            model.ModelName,
            model.ModelNumber,
            model.ReleaseYear,
            model.IsActive,
            model.CreatedAt,
            model.UpdatedAt);
}
