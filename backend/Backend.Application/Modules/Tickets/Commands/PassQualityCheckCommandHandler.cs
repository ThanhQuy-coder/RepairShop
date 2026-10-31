using MediatR;
using Microsoft.Extensions.Logging;
using RepairShop.Application.Common.Authorization;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Tickets;
using RepairShop.Application.Modules.Tickets.DTOs;
using RepairShop.Domain.Common;
using RepairShop.Domain.Modules.Inspections;

public class PassQualityCheckCommandHandler : IRequestHandler<PassQualityCheckCommand, TicketResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly IRepairStatusRepository _statusRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ICustomerRepository _customerRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<PassQualityCheckCommandHandler> _logger;
    private readonly IInspectionRepository _inspectionRepository;

    public PassQualityCheckCommandHandler(IRepairTicketRepository ticketRepository,
        IRepairStatusRepository statusRepository,
        ICurrentUserService currentUser,
        ICustomerRepository customerRepository,
        INotificationService notificationService,
        ILogger<PassQualityCheckCommandHandler> logger,
        IInspectionRepository inspectionRepository)
    {
        _ticketRepository = ticketRepository;
        _statusRepository = statusRepository;
        _currentUser = currentUser;
        _customerRepository = customerRepository;
        _notificationService = notificationService;
        _logger = logger;
        _inspectionRepository = inspectionRepository;
    }

    public async Task<TicketResponse> Handle(PassQualityCheckCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        TicketAccessGuard.EnsureCanAccess(ticket, _currentUser);

        var userId = _currentUser.UserId!.Value;

        await EnsureInspectionsCompleteAsync(ticket.Id);

        // Ghi 3 hạng mục kiểm tra như 1 note có cấu trúc — MVP tối thiểu, không tạo bảng QaChecklist riêng
        // (đúng tinh thần "chưa cần Inventory hoàn chỉnh" áp dụng tương tự cho QA: đủ dùng, không over-engineer)
        var qaSummary =
            $"[QA - Chức năng] {request.FunctionalCheckNotes}\n" +
            $"[QA - Ngoại hình] {request.CosmeticCheckNotes}\n" +
            $"[QA - Lỗi ban đầu] {request.OriginalIssueResolvedNotes}";
        ticket.AddNote(qaSummary);

        var readyStatus = await _statusRepository.GetByCodeAsync(RepairStatusCodes.ReadyForPickup);

        // BR-19 enforce ở đây (Task 4.2): PassQualityCheck() tự kiểm tra ticket đã từng IN_REPAIR chưa
        ticket.PassQualityCheck(readyStatus, userId, "QA đạt — thiết bị sẵn sàng bàn giao");

        _ticketRepository.TrackNewStatusHistory(ticket.StatusHistories.Last());
        await _ticketRepository.SaveChangesAsync();

        await TicketNotificationHelper.NotifyCustomerIfLinkedAsync(
            _customerRepository, _notificationService, ticket.CustomerId,
            NotificationTypes.TicketReadyForPickup, "Thiết bị đã sẵn sàng bàn giao",
            $"Phiếu {ticket.TicketCode} đã kiểm thử xong, mời bạn đến cửa hàng nhận máy.", ticket.Id);


        _logger.LogInformation("Ticket {TicketCode} QA ĐẠT, chuyển READY_FOR_PICKUP", ticket.TicketCode);

        return TicketMapper.ToResponse(ticket);
    }

    private async Task EnsureInspectionsCompleteAsync(Guid ticketId)
    {
        var functional = await _inspectionRepository.GetResultAsync(ticketId, InspectionType.Functional);
        var appearance = await _inspectionRepository.GetResultAsync(ticketId, InspectionType.Appearance);
        var verification = await _inspectionRepository.GetResultAsync(ticketId, InspectionType.RepairVerification);
        if (functional is null || appearance is null || verification is null)
            throw new InvalidOperationException("Vui lòng hoàn tất đủ 3 checklist trước khi QA đạt.");
        if (functional.Items.Any(x => x.Status is "Fail" or "NotTested") ||
            appearance.Items.Any(x => x.Status == "Unknown") ||
            verification.Items.Any(x => x.Status != "Fixed"))
            throw new InvalidOperationException("Checklist còn mục chưa đạt hoặc chưa xác nhận, không thể QA đạt.");
    }
}