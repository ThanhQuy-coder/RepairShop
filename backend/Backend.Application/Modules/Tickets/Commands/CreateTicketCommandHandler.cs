using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Tickets.DTOs;
using RepairShop.Domain.Common;
using RepairShop.Domain.Modules.Tickets;
using MediatR;
using Microsoft.Extensions.Logging;
using RepairShop.Application.Modules.Tickets.Commands;

namespace RepairShop.Application.Modules.Tickets.Commands;

public class CreateTicketCommandHandler : IRequestHandler<CreateTicketCommand, TicketResponse>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly IRepairStatusRepository _statusRepository;
    private readonly ITicketCodeGenerator _codeGenerator;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CreateTicketCommandHandler> _logger;

    public CreateTicketCommandHandler(
        ICustomerRepository customerRepository,
        IDeviceRepository deviceRepository,
        IRepairTicketRepository ticketRepository,
        IRepairStatusRepository statusRepository,
        ITicketCodeGenerator codeGenerator,
        ICurrentUserService currentUser,
        ILogger<CreateTicketCommandHandler> logger)
    {
        _customerRepository = customerRepository;
        _deviceRepository = deviceRepository;
        _ticketRepository = ticketRepository;
        _statusRepository = statusRepository;
        _codeGenerator = codeGenerator;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<TicketResponse> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate Customer
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId)
            ?? throw new NotFoundException("Khách hàng", request.CustomerId);

        // 2. Validate Device
        var device = await _deviceRepository.GetByIdAsync(request.DeviceId)
            ?? throw new NotFoundException("Thiết bị", request.DeviceId);

        // 3. Kiểm tra Device thuộc Customer — chặn Customer A dùng Device của Customer B
        if (device.CustomerId != customer.Id)
            throw new DeviceOwnershipMismatchException(device.Id, customer.Id);

        // 4. Sinh Ticket Code (đã tự kiểm tra trùng bên trong TicketCodeGenerator)
        var ticketCode = await _codeGenerator.GenerateUniqueCodeAsync();

        // 5+6. Tạo Ticket + gán trạng thái ban đầu (constructor RepairTicket ép buộc phải là CHECKED_IN — Task 4.1)
        var checkedInStatus = await _statusRepository.GetByCodeAsync(RepairStatusCodes.CheckedIn);

        var receptionistId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Không xác định được nhân viên tiếp nhận từ token.");

        var ticket = new RepairTicket(
            ticketCode,
            customer.Id,
            device.Id,
            receptionistId,
            request.IssueDescription,
            checkedInStatus);

        if (!string.IsNullOrWhiteSpace(request.Notes))
            ticket.AddNote(request.Notes);

        // 7. Ghi Status History — xảy ra TỰ ĐỘNG bên trong constructor RepairTicket (gán StatusId + Status
        //    trực tiếp, KHÔNG qua ChangeStatus()); cần bổ sung thủ công 1 bản ghi lịch sử "khởi tạo" ở đây
        //    vì constructor không tự thêm StatusHistory (chỉ ChangeStatus() mới làm việc đó — xem Task 4.2).
        ticket.RecordInitialStatusHistory(receptionistId);

        await _ticketRepository.AddAsync(ticket);
        await _ticketRepository.SaveChangesAsync();

        _logger.LogInformation("Tạo mới RepairTicket {TicketCode} cho Customer {CustomerId}, Device {DeviceId}",
            ticket.TicketCode, customer.Id, device.Id);

        // 8. Trả Ticket
        return new TicketResponse(ticket.Id, ticket.TicketCode, ticket.CustomerId, ticket.DeviceId,
            ticket.Status.Code, ticket.IssueReported, ticket.Notes, ticket.ReceivedAt);
    }
}