using MediatR;
using Microsoft.Extensions.Logging;
using RepairShop.Application.Common.Authorization;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Modules.Tickets;

public class UsePartCommandHandler : IRequestHandler<UsePartCommand, UsePartResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly IPartRepository _partRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UsePartCommandHandler> _logger;

    public UsePartCommandHandler(IRepairTicketRepository ticketRepository, IPartRepository partRepository,
        IInventoryRepository inventoryRepository, ICurrentUserService currentUser,
        ILogger<UsePartCommandHandler> logger, IUnitOfWork unitOfWork)
    {
        _ticketRepository = ticketRepository;
        _partRepository = partRepository;
        _inventoryRepository = inventoryRepository;
        _currentUser = currentUser;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<UsePartResponse> Handle(UsePartCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        TicketAccessGuard.EnsureCanAccess(ticket, _currentUser);

        var part = await _partRepository.GetByIdAsync(request.PartId)
            ?? throw new NotFoundException("Linh kiện", request.PartId);

        var inventory = await _inventoryRepository.GetByPartIdAsync(part.Id)
            ?? throw new NotFoundException("Tồn kho của linh kiện", part.Id);

        var userId = _currentUser.UserId!.Value;
        TicketPart? newTicketPart = null;

        // Bọc "trừ Inventory + tạo TicketPart" trong 1 transaction: nếu Deduct() thành công nhưng
        // SaveChanges thất bại giữa chừng, KHÔNG được để tồn kho đã trừ mà TicketPart lại chưa tồn tại
        // (mất dấu vết linh kiện đã xuất, không đối soát được).
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            ticket.UsePart(part, inventory, request.Quantity, userId); // BR-20 enforce ở đây

            newTicketPart = ticket.TicketParts.Last();
            _ticketRepository.TrackNewTicketPart(newTicketPart);

            await _ticketRepository.SaveChangesAsync();
        }, cancellationToken);

        _logger.LogInformation("Ghi nhận sử dụng {Quantity}x {PartName} cho Ticket {TicketCode}",
            request.Quantity, part.Name, ticket.TicketCode);

        return new UsePartResponse(newTicketPart!.Id, part.Name, newTicketPart.Quantity,
            newTicketPart.UnitPriceAtUse, newTicketPart.Subtotal);
    }
}