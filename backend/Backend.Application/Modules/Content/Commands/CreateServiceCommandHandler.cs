using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Content.DTOs;
using RepairShop.Application.Modules.Content.Validators;
using RepairShop.Domain.Modules.Content;
using MediatR;

namespace RepairShop.Application.Modules.Content.Commands;

public class CreateServiceCommandHandler : IRequestHandler<CreateServiceCommand, ServiceResponse>
{
    private readonly IServiceRepository _serviceRepository;
    private readonly IFileStorageService? _fileStorageService;

    public CreateServiceCommandHandler(
        IServiceRepository serviceRepository,
        IFileStorageService? fileStorageService = null)
    {
        _serviceRepository = serviceRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<ServiceResponse> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
    {
        var imageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();

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

            imageUrl = uploaded.Url;
        }

        var service = new Service(request.Name, request.Description, request.BasePrice, request.DeviceType, imageUrl);
        await _serviceRepository.AddAsync(service);
        await _serviceRepository.SaveChangesAsync();

        return new ServiceResponse(
            service.Id,
            service.Name,
            service.Description,
            service.BasePrice,
            service.DeviceType,
            service.IsActive,
            service.ImageUrl,
            0,
            0);
    }
}
