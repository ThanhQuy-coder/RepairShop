using RepairShop.Domain.Modules.Tickets.Enums;
using MediatR;

namespace RepairShop.Application.Modules.Tickets.Commands;

public record UploadTicketImageCommand(
    Guid TicketId,
    Stream FileStream,
    string FileName,
    ImageType ImageType,
    string? Caption) : IRequest<TicketImageResponse>;

public record TicketImageResponse(Guid Id, string ImageUrl, string ImageType, DateTime UploadedAt);