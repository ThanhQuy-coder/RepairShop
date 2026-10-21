using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Appointments.DTOs;
using RepairShop.Domain.Modules.Appointments;
using System.Security.Cryptography;
using MediatR;
using Microsoft.Extensions.Logging;
using RepairShop.Domain.Common.Exceptions;
using RepairShop.Domain.Common;

namespace RepairShop.Application.Modules.Appointments.Commands;

public class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, AppointmentResponse>
{
    private readonly ITimeSlotConfigRepository _slotRepo;
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateAppointmentCommandHandler> _logger;
    private readonly IBusinessClock _clock;
    private readonly IOtpGenerator _otpGenerator;

    public CreateAppointmentCommandHandler(ITimeSlotConfigRepository slotRepo, IAppointmentRepository appointmentRepo,
        INotificationService notificationService, IUnitOfWork unitOfWork, ILogger<CreateAppointmentCommandHandler> logger,
        IBusinessClock clock, IOtpGenerator otpGenerator)
    {
        _slotRepo = slotRepo;
        _appointmentRepo = appointmentRepo;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _clock = clock;
        _otpGenerator = otpGenerator;
    }

    public async Task<AppointmentResponse> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        // 2. TimeSlot tồn tại
        var slot = await _slotRepo.GetByIdAsync(request.TimeSlotId)
            ?? throw new NotFoundException("Khung giờ", request.TimeSlotId);

        // 3. TimeSlot active
        if (!slot.IsActive)
            throw new DomainException("Khung giờ này hiện không còn hoạt động.");

        // 4. Ngày hợp lệ (ngoài check FluentValidation) — kiểm tra thêm slot có áp dụng đúng thứ trong tuần không
        if (!slot.AppliesTo(request.AppointmentDate))
            throw new DomainException("Khung giờ này không áp dụng cho ngày đã chọn.");

        var requestedLocalTime = request.AppointmentDate.ToDateTime(slot.SlotStart);
        if (requestedLocalTime <= _clock.Now.DateTime)
            throw new DomainException("Thời gian hẹn phải ở tương lai theo múi giờ cửa hàng.");

        Appointment? appointment = null;
        var lookupToken = _otpGenerator.GenerateNumericOtp();
        var lookupHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(lookupToken)));

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Khóa đồng bộ TRƯỚC khi đếm — chặn mọi request khác cùng (date, slot) cho tới khi transaction này xong
            await _appointmentRepo.AcquireSlotLockAsync(request.AppointmentDate, request.TimeSlotId, cancellationToken);

            // 5. Đếm appointment hiện tại (PENDING + CONFIRMED)
            var booked = await _appointmentRepo.CountBookedAsync(request.AppointmentDate, request.TimeSlotId);

            // 6. Kiểm tra capacity
            if (booked >= slot.MaxCapacity)
                throw new DomainException("SLOT_FULL: Khung giờ này đã đầy, vui lòng chọn khung giờ khác.");

            // 7. Tạo Appointment PENDING (mặc định trong constructor)
            var normalizedPhone = new string(request.Phone.Where(char.IsDigit).ToArray());
            appointment = new Appointment(request.FullName, normalizedPhone, request.AppointmentDate, request.TimeSlotId,
                request.CustomerId, request.DeviceType, request.Brand, request.Model, request.IssueDescription, lookupHash);

            await _appointmentRepo.AddAsync(appointment);

            // 8. Tạo Notification cho Receptionist — cùng transaction, cùng sống/chết với Appointment
            await _notificationService.CreateForRoleAsync("Receptionist", NotificationTypes.AppointmentCreated,
                "Lịch hẹn mới", $"{request.FullName} vừa đặt lịch ngày {request.AppointmentDate:dd/MM/yyyy}.",
                "Appointment", appointment.Id);

            await _appointmentRepo.SaveChangesAsync();
        }, cancellationToken);

        _logger.LogInformation("Tạo Appointment {AppointmentId} cho {FullName} ngày {Date}, slot {SlotId}",
            appointment!.Id, appointment.FullName, appointment.AppointmentDate, request.TimeSlotId);

        return new AppointmentResponse(appointment.Id, Guid.Empty, appointment.FullName, appointment.Phone,
            appointment.AppointmentDate, appointment.TimeSlotId, appointment.Status.ToString(), appointment.CreatedAt, lookupToken);
    }
}