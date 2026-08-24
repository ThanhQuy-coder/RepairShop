using MediatR;
using Microsoft.Extensions.Logging;
using RepairShop.Application.Common.Authorization;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;

public class UsePartCommandHandler : IRequestHandler<UsePartCommand, UsePartResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly IPartRepository _partRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<UsePartCommandHandler> _logger;

    public UsePartCommandHandler(IRepairTicketRepository ticketRepository, IPartRepository partRepository,
        IInventoryRepository inventoryRepository, ICurrentUserService currentUser,
        ILogger<UsePartCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _partRepository = partRepository;
        _inventoryRepository = inventoryRepository;
        _currentUser = currentUser;
        _logger = logger;
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

        // BR-20 enforce thật ở đây: RepairTicket.UsePart() -> Inventory.Deduct() -> false nếu thiếu
        // -> ném InsufficientStockException (409), Application KHÔNG tự kiểm tra tồn kho tay lần nữa,
        // tránh trùng logic — Domain là nơi DUY NHẤT quyết định "đủ hay không".
        ticket.UsePart(part, inventory, request.Quantity, userId);

        var newTicketPart = ticket.TicketParts.Last();
        _ticketRepository.TrackNewTicketPart(newTicketPart); // ticket đã tracked Unchanged -> track tường minh
        // inventory là aggregate ĐỘC LẬP, được load trực tiếp qua IInventoryRepository (không qua navigation
        // của ticket) -> EF Core tự phát hiện Modified bình thường, KHÔNG cần TrackNewX cho inventory.

        await _ticketRepository.SaveChangesAsync();

        _logger.LogInformation("Ghi nhận sử dụng {Quantity}x {PartName} cho Ticket {TicketCode}",
            request.Quantity, part.Name, ticket.TicketCode);

        return new UsePartResponse(newTicketPart.Id, part.Name, newTicketPart.Quantity,
            newTicketPart.UnitPriceAtUse, newTicketPart.Subtotal);
    }
}