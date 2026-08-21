using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace RepairShop.Application.Modules.Tickets.Commands;

public class UploadTicketImageCommandHandler : IRequestHandler<UploadTicketImageCommand, TicketImageResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<UploadTicketImageCommandHandler> _logger;

    public UploadTicketImageCommandHandler(
        IRepairTicketRepository ticketRepository,
        IFileStorageService fileStorageService,
        ICurrentUserService currentUser,
        ILogger<UploadTicketImageCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _fileStorageService = fileStorageService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<TicketImageResponse> Handle(UploadTicketImageCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Không xác định được người upload từ token.");

        // Upload lên Cloudinary TRƯỚC — nếu Domain reject (VD sai trạng thái) thì ảnh coi như "mồ côi" trên
        // Cloudinary (chấp nhận đánh đổi ở quy mô đồ án; hệ thống production thật sẽ cần cơ chế dọn rác/rollback).
        var uploadResult = await _fileStorageService.UploadImageAsync(
            request.FileStream, request.FileName, folder: $"repairshop/tickets/{ticket.TicketCode}");

        // Domain tự enforce: đúng loại ảnh + đúng trạng thái mới cho thêm (Task 4.5)
        ticket.AddImage(uploadResult.Url, request.ImageType, userId, request.Caption);

        await _ticketRepository.SaveChangesAsync();

        _logger.LogInformation("Upload ảnh {ImageType} cho ticket {TicketCode}", request.ImageType, ticket.TicketCode);

        var image = ticket.Images.Last();
        return new TicketImageResponse(image.Id, image.ImageUrl, image.ImageType.ToString(), image.UploadedAt);
    }
}