using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
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
    private readonly ILogger<CreateWarrantyClaimCommandHandler> _logger;

    public CreateWarrantyClaimCommandHandler(
        IRepairTicketRepository ticketRepository, IRepairStatusRepository statusRepository,
        ITicketCodeGenerator codeGenerator, ICurrentUserService currentUser,
        ILogger<CreateWarrantyClaimCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _statusRepository = statusRepository;
        _codeGenerator = codeGenerator;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<TicketResponse> Handle(CreateWarrantyClaimCommand request, CancellationToken cancellationToken)
    {
        var parentTicket = await _ticketRepository.GetByIdAsync(request.ParentTicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa gốc", request.ParentTicketId);

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
        var receptionistId = _currentUser.UserId!.Value;

        // Ticket MỚI, liên kết ParentTicketId về ticket gốc (BR-12) — không tính DiagnosticDeposit
        // mặc định cho ca bảo hành (đúng mô tả Task 1 Tuần 1: "không tính phí, hoặc tính phí tùy lỗi").
        var claimTicket = new RepairTicket(
            ticketCode, parentTicket.CustomerId, parentTicket.DeviceId, receptionistId,
            request.IssueReported, checkedInStatus, diagnosticDeposit: 0, parentTicketId: parentTicket.Id);

        claimTicket.RecordInitialStatusHistory(receptionistId);

        await _ticketRepository.AddAsync(claimTicket);
        await _ticketRepository.SaveChangesAsync();

        _logger.LogInformation("Tạo Warranty Claim {ClaimCode} liên kết ticket gốc {ParentCode}",
            claimTicket.TicketCode, parentTicket.TicketCode);

        return TicketMapper.ToResponse(claimTicket);
    }
}