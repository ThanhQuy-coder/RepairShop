using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Tickets.DTOs;
using RepairShop.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using RepairShop.Domain.Common.Exceptions;

namespace RepairShop.Application.Modules.Tickets.Commands;

public class AssignTechnicianCommandHandler : IRequestHandler<AssignTechnicianCommand, TicketResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRepairStatusRepository _statusRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AssignTechnicianCommandHandler> _logger;

    public AssignTechnicianCommandHandler(
        IRepairTicketRepository ticketRepository,
        IUserRepository userRepository,
        IRepairStatusRepository statusRepository,
        ICurrentUserService currentUser,
        ILogger<AssignTechnicianCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _statusRepository = statusRepository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<TicketResponse> Handle(AssignTechnicianCommand request, CancellationToken cancellationToken)
    {
        // 3. Ticket tồn tại
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        // 1. Technician tồn tại
        var technician = await _userRepository.GetByIdAsync(request.TechnicianId)
            ?? throw new NotFoundException("Kỹ thuật viên", request.TechnicianId);

        // 2. User thực sự có Role Technician — KHÔNG cho gán nhầm 1 Receptionist/Admin vào làm kỹ thuật viên
        if (technician.Role?.Name != Roles.Technician)
            throw new DomainException(
                $"User '{technician.FullName}' không có vai trò Technician, không thể phân công sửa chữa.");

        if (!technician.IsActive)
            throw new DomainException($"Kỹ thuật viên '{technician.FullName}' hiện đã bị khóa tài khoản.");

        var assignedStatus = await _statusRepository.GetByCodeAsync(RepairStatusCodes.Assigned);

        var assignerId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Không xác định được người thực hiện thao tác từ token.");

        // 4. Ticket đang ở trạng thái cho phép assign — enforce TỰ ĐỘNG bởi RepairTicketStateMachine
        //    (Task 4.2) bên trong ticket.AssignTechnician() -> ChangeStatus() -> EnsureCanTransition()
        //    Nếu ticket không ở CHECKED_IN, DomainException sẽ tự ném ra ở đây, KHÔNG cần check tay lại.
        ticket.AssignTechnician(technician.Id, assignedStatus, assignerId, request.Note);

        var newHistory = ticket.StatusHistories.Last();
        _ticketRepository.TrackNewStatusHistory(newHistory); // fix Guid-tracking đã áp dụng ở Bước 0

        await _ticketRepository.SaveChangesAsync();

        _logger.LogInformation("Gán Technician {TechnicianId} cho Ticket {TicketCode}",
            technician.Id, ticket.TicketCode);

        return new TicketResponse(ticket.Id, ticket.TicketCode, ticket.CustomerId, ticket.DeviceId,
            ticket.Status.Code, ticket.IssueReported, ticket.Notes, ticket.ConditionNotes, ticket.RiskWarning,
            ticket.ReceivedAt);

        // 5. "Technician có quyền thao tác Ticket sau khi được assign" — đây là hệ quả của việc gán
        //    TechnicianId vào ticket; việc ENFORCE quyền đó xảy ra ở các API TIẾP THEO (StartDiagnosis,
        //    SubmitDiagnosis...) thông qua TicketAccessGuard.EnsureCanAccess(), minh hoạ ở Bước 4 dưới đây.
    }
}