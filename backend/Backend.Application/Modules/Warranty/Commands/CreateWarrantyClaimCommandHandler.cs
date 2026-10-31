using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Common.Authorization;
using RepairShop.Domain.Common;
using RepairShop.Domain.Modules.Tickets;
using MediatR;
using Microsoft.Extensions.Logging;
using RepairShop.Application.Modules.Tickets.DTOs;
using RepairShop.Domain.Common.Exceptions;
using RepairShop.Application.Modules.Tickets;

namespace RepairShop.Application.Modules.Warranty.Commands;

public class CreateWarrantyClaimCommandHandler : IRequestHandler<CreateWarrantyClaimCommand, TicketResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly IRepairStatusRepository _statusRepository;
    private readonly ITicketCodeGenerator _codeGenerator;
    private readonly ICurrentUserService _currentUser;
    private readonly ICustomerRepository _customerRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CreateWarrantyClaimCommandHandler> _logger;

    public CreateWarrantyClaimCommandHandler(
        IRepairTicketRepository ticketRepository, IRepairStatusRepository statusRepository,
        ITicketCodeGenerator codeGenerator, ICurrentUserService currentUser,
        ICustomerRepository customerRepository, INotificationService notificationService,
        ILogger<CreateWarrantyClaimCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _statusRepository = statusRepository;
        _codeGenerator = codeGenerator;
        _currentUser = currentUser;
        _customerRepository = customerRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<TicketResponse> Handle(CreateWarrantyClaimCommand request, CancellationToken cancellationToken)
    {
        var parentTicket = await _ticketRepository.GetByIdAsync(request.ParentTicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa gốc", request.ParentTicketId);

        if (_currentUser.Role == Roles.Customer)
        {
            var customer = await _customerRepository.GetByUserIdAsync(_currentUser.UserId!.Value);
            TicketAccessGuard.EnsureCustomerOwnsTicket(parentTicket, customer);
        }

        // Test bắt buộc theo checklist: Warranty exists? -> Is active? -> Expired? -> Eligible?
        // Toàn bộ 4 bước này được enforce TRONG DOMAIN (EnsureCanCreateWarrantyClaim), không lặp
        // lại logic ở Application — nếu bất kỳ điều kiện nào fail, DomainException ném ra rõ ràng.
        parentTicket.EnsureCanCreateWarrantyClaim();

        // Check duplicate claim: không cho tạo 2 claim ticket đang mở cùng lúc từ 1 ticket gốc
        var hasOpenClaim = parentTicket.WarrantyTickets.Any(t =>
            t.Status.Code != RepairStatusCodes.Delivered && t.Status.Code != RepairStatusCodes.ClosedRejected);
        if (hasOpenClaim)
            throw new DomainException("Ticket này đã có yêu cầu bảo hành đang được xử lý.");

        var ticketCode = await _codeGenerator.GenerateUniqueCodeAsync();
        var checkedInStatus = await _statusRepository.GetByCodeAsync(RepairStatusCodes.CheckedIn);
        // Ticket MỚI, liên kết ParentTicketId về ticket gốc (BR-12) — không tính DiagnosticDeposit
        // mặc định cho ca bảo hành (đúng mô tả Task 1 Tuần 1: "không tính phí, hoặc tính phí tùy lỗi").
        var claimTicket = new RepairTicket(
            ticketCode, parentTicket.CustomerId, parentTicket.DeviceId, parentTicket.ReceptionistId,
            request.IssueReported.Trim(), checkedInStatus, diagnosticDeposit: 0, parentTicketId: parentTicket.Id);

        claimTicket.RecordInitialStatusHistory(parentTicket.ReceptionistId);

        await _ticketRepository.AddAsync(claimTicket);
        await _ticketRepository.SaveChangesAsync();

        await _notificationService.CreateForRoleAsync(
            "Receptionist",
            NotificationTypes.WarrantyClaimCreated,
            "Có yêu cầu bảo hành mới",
            $"Khách hàng yêu cầu bảo hành cho ticket {parentTicket.TicketCode}. Ticket tiếp nhận: {claimTicket.TicketCode}.",
            "Ticket",
            claimTicket.Id);

        _logger.LogInformation("Tạo Warranty Claim {ClaimCode} liên kết ticket gốc {ParentCode}",
            claimTicket.TicketCode, parentTicket.TicketCode);

        return TicketMapper.ToResponse(claimTicket);
    }
}