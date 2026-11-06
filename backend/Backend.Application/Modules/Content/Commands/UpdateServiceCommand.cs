using FluentValidation;
using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Content.DTOs;
using RepairShop.Application.Modules.Content.Validators;

namespace RepairShop.Application.Modules.Content.Commands;

public record UpdateServiceCommand(
    Guid Id,
    string Name,
    string? Description,
    decimal? BasePrice,
    string? DeviceType,
    string? ImageUrl = null,
    Stream? ImageStream = null,
    string? ImageFileName = null,
    string? ImageContentType = null,
    long? ImageLength = null,
    bool RemoveImage = false)
    : IRequest<ServiceResponse>;

public record UploadServiceImageCommand(
    Stream FileStream,
    string FileName,
    string? ContentType = null,
    long? FileLength = null) : IRequest<UploadedFileResult>;

public class UpdateServiceCommandValidator : AbstractValidator<UpdateServiceCommand>
{
    private static readonly string[] AllowedDeviceTypes = ["Phone", "Laptop", "Tablet", "Smartwatch", "Electronics"];

    public UpdateServiceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên dịch vụ không được để trống.")
            .MaximumLength(150).WithMessage("Tên dịch vụ tối đa 150 ký tự.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Mô tả dịch vụ tối đa 2000 ký tự.");

        RuleFor(x => x.BasePrice)
            .GreaterThanOrEqualTo(0).When(x => x.BasePrice.HasValue)
            .WithMessage("Giá dịch vụ không thể âm.");

        RuleFor(x => x.DeviceType)
            .Must(dt => string.IsNullOrWhiteSpace(dt) || AllowedDeviceTypes.Contains(dt.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("Loại thiết bị phải là Phone, Laptop, Tablet, Smartwatch hoặc Electronics.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage("Đường dẫn ảnh tối đa 500 ký tự.");
    }
}

public class UpdateServiceCommandHandler :
    IRequestHandler<UpdateServiceCommand, ServiceResponse>,
    IRequestHandler<UploadServiceImageCommand, UploadedFileResult>
{
    private readonly IServiceRepository _serviceRepository;
    private readonly IFileStorageService? _fileStorageService;

    public UpdateServiceCommandHandler(
        IServiceRepository serviceRepository,
        IFileStorageService? fileStorageService = null)
    {
        _serviceRepository = serviceRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<ServiceResponse> Handle(UpdateServiceCommand request, CancellationToken cancellationToken)
    {
        var service = await _serviceRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Dịch vụ", request.Id);

        var oldImageUrl = service.ImageUrl;
        var finalImageUrl = service.ImageUrl;

        if (request.ImageStream is not null)
        {
            var safeFileName = ImageFileValidator.ValidateAndBuildSafeFileName(
                request.ImageStream,
                request.ImageFileName,
                request.ImageContentType,
                request.ImageLength);

            if (_fileStorageService is null)
                throw new InvalidOperationException("Dịch vụ lưu trữ file chưa được cấu hình.");

            var uploaded = await _fileStorageService.UploadImageAsync(
                request.ImageStream,
                safeFileName,
                "fixora/services");

            finalImageUrl = uploaded.Url;

            if (!string.IsNullOrWhiteSpace(oldImageUrl) &&
                !string.Equals(oldImageUrl, finalImageUrl, StringComparison.OrdinalIgnoreCase))
            {
                await _fileStorageService.DeleteImageAsync(oldImageUrl);
            }
        }
        else if (request.RemoveImage)
        {
            finalImageUrl = null;
            if (!string.IsNullOrWhiteSpace(oldImageUrl) && _fileStorageService is not null)
            {
                await _fileStorageService.DeleteImageAsync(oldImageUrl);
            }
        }
        else if (request.ImageUrl is not null)
        {
            finalImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
            if (!string.IsNullOrWhiteSpace(oldImageUrl) &&
                !string.Equals(oldImageUrl, finalImageUrl, StringComparison.OrdinalIgnoreCase) &&
                _fileStorageService is not null)
            {
                await _fileStorageService.DeleteImageAsync(oldImageUrl);
            }
        }

        service.UpdateInfo(request.Name, request.Description, request.BasePrice, request.DeviceType, finalImageUrl);
        _serviceRepository.Update(service);
        await _serviceRepository.SaveChangesAsync();

        var ratingStats = await _serviceRepository.GetServiceRatingStatsAsync([service.Id]);
        var (avgRating, reviewCount) = ratingStats.GetValueOrDefault(service.Id, (0.0, 0));

        return new ServiceResponse(
            service.Id,
            service.Name,
            service.Description,
            service.BasePrice,
            service.DeviceType,
            service.IsActive,
            service.ImageUrl,
            avgRating,
            reviewCount);
    }

    public async Task<UploadedFileResult> Handle(UploadServiceImageCommand request, CancellationToken cancellationToken)
    {
        var safeFileName = ImageFileValidator.ValidateAndBuildSafeFileName(
            request.FileStream,
            request.FileName,
            request.ContentType,
            request.FileLength);

        if (_fileStorageService is null)
            throw new InvalidOperationException("Dịch vụ lưu trữ file chưa được cấu hình.");

        return await _fileStorageService.UploadImageAsync(
            request.FileStream,
            safeFileName,
            "fixora/services");
    }
}
