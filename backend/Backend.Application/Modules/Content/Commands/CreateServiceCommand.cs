using FluentValidation;
using RepairShop.Application.Modules.Content.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Content.Commands;

public record CreateServiceCommand(
    string Name,
    string? Description,
    decimal? BasePrice,
    string? DeviceType,
    string? ImageUrl = null,
    Stream? ImageStream = null,
    string? ImageFileName = null,
    string? ImageContentType = null,
    long? ImageLength = null)
    : IRequest<ServiceResponse>;

public class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    private static readonly string[] AllowedDeviceTypes = ["Phone", "Laptop", "Tablet", "Smartwatch", "Electronics"];

    public CreateServiceCommandValidator()
    {
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
