using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Tickets;
using RepairShop.Application.Modules.Tickets.DTOs;
using RepairShop.Domain.Common;
using System.Collections.Generic;

public record SubmitDiagnosisCommand(
    Guid TicketId,
    string? DiagnosisResult,
    string? RootCause,
    string? RecommendedRepair,
    string? RequiredPartsNote,
    string? TechnicalNote,
    IReadOnlyList<Guid>? DiagnosisSymptomIds = null,
    string? DiagnosisResultOther = null,
    IReadOnlyList<Guid>? RequiredPartIds = null,
    IReadOnlyList<Guid>? RecommendedServiceIds = null) : IRequest<TicketResponse>;

public class SubmitDiagnosisCommandHandler : IRequestHandler<SubmitDiagnosisCommand, TicketResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IRepairStatusRepository _statusRepository;
    private readonly IDiagnosisSymptomRepository _symptomRepository;
    private readonly IPartRepository _partRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly INotificationService _notificationService;

    public SubmitDiagnosisCommandHandler(
        IRepairTicketRepository ticketRepository,
        ICurrentUserService currentUser,
        IRepairStatusRepository statusRepository,
        IDiagnosisSymptomRepository symptomRepository,
        IPartRepository partRepository,
        IInventoryRepository inventoryRepository,
        IServiceRepository serviceRepository,
        INotificationService notificationService)
    {
        _ticketRepository = ticketRepository;
        _currentUser = currentUser;
        _statusRepository = statusRepository;
        _symptomRepository = symptomRepository;
        _partRepository = partRepository;
        _inventoryRepository = inventoryRepository;
        _serviceRepository = serviceRepository;
        _notificationService = notificationService;
    }

    public async Task<TicketResponse> Handle(SubmitDiagnosisCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        RepairShop.Application.Common.Authorization.TicketAccessGuard.EnsureCanAccess(ticket, _currentUser);

        if (ticket.Status.Code == RepairStatusCodes.Assigned)
        {
            var diagnosingStatus = await _statusRepository.GetByCodeAsync(RepairStatusCodes.Diagnosing);
            ticket.StartDiagnosis(diagnosingStatus, _currentUser.UserId!.Value);
            _ticketRepository.TrackNewStatusHistory(ticket.StatusHistories.Last());
        }

        var diagnosisResult = await BuildDiagnosisResultAsync(request, ticket.Device.DeviceType);
        var requiredPartsNote = await BuildRequiredPartsNoteAsync(request.RequiredPartIds);
        var recommendedRepair = await BuildRecommendedRepairAsync(request.RecommendedServiceIds);

        // Không đổi trạng thái — chỉ ghi dữ liệu, nên KHÔNG tạo StatusHistory, không cần TrackNewX
        ticket.SubmitDiagnosis(diagnosisResult, request.RootCause,
            recommendedRepair, requiredPartsNote);

        if (!string.IsNullOrWhiteSpace(request.TechnicalNote))
            ticket.AddNote(request.TechnicalNote);

        await _ticketRepository.SaveChangesAsync();

        await _notificationService.CreateAsync(
            ticket.ReceptionistId,
            NotificationTypes.DiagnosisCompleted,
            "Kỹ thuật viên đã hoàn tất chẩn đoán",
            $"Phiếu {ticket.TicketCode} đã có kết quả chẩn đoán, vui lòng kiểm tra và lập báo giá.",
            "Ticket",
            ticket.Id);

        return TicketMapper.ToResponse(ticket);
    }

    private async Task<string> BuildDiagnosisResultAsync(
        SubmitDiagnosisCommand request, RepairShop.Domain.Common.Enums.DeviceType deviceType)
    {
        var ids = request.DiagnosisSymptomIds ?? [];
        var symptoms = await _symptomRepository.GetByIdsAsync(ids);
        if (symptoms.Count != ids.Distinct().Count() ||
            symptoms.Any(s => !s.IsActive || s.DeviceType != deviceType))
            throw new InvalidOperationException("Danh sách triệu chứng chẩn đoán không hợp lệ.");

        var values = symptoms.OrderBy(s => s.SortOrder).Select(s => s.Name).ToList();
        if (!string.IsNullOrWhiteSpace(request.DiagnosisResultOther))
            values.Add($"Khác: {request.DiagnosisResultOther.Trim()}");
        if (values.Count == 0 && !string.IsNullOrWhiteSpace(request.DiagnosisResult))
            values.Add(request.DiagnosisResult.Trim());
        if (values.Count == 0)
            throw new InvalidOperationException("Vui lòng chọn ít nhất một triệu chứng hoặc nhập nội dung khác.");
        return string.Join("; ", values);
    }

    private async Task<string?> BuildRequiredPartsNoteAsync(IReadOnlyList<Guid>? ids)
    {
        if (ids is null || ids.Count == 0) return null;
        var parts = await _partRepository.GetByIdsAsync(ids);
        if (parts.Count != ids.Distinct().Count() || parts.Any(p => !p.IsActive))
            throw new InvalidOperationException("Danh sách linh kiện không hợp lệ.");

        foreach (var part in parts)
        {
            var inventory = await _inventoryRepository.GetByPartIdAsync(part.Id);
            if (inventory is null || inventory.QuantityOnHand <= 0)
                throw new InvalidOperationException($"Linh kiện '{part.Name}' hiện không còn tồn kho.");
        }

        return string.Join("; ", parts.OrderBy(p => p.Name).Select(p => $"{p.Name} ({p.Sku})"));
    }

    private async Task<string?> BuildRecommendedRepairAsync(IReadOnlyList<Guid>? ids)
    {
        if (ids is null || ids.Count == 0) return null;
        var services = await _serviceRepository.GetByIdsAsync(ids);
        if (services.Count != ids.Distinct().Count() || services.Any(s => !s.IsActive))
            throw new InvalidOperationException("Danh sách dịch vụ sửa chữa không hợp lệ.");
        return string.Join("; ", services.OrderBy(s => s.Name).Select(s => s.Name));
    }
}