using MediatR;
using RepairShop.Application.Modules.AIAdvisory.DTOs;

namespace RepairShop.Application.Modules.AIAdvisory.Commands.GetConsultation;

/// <summary>
/// Task 4.1 — Command tư vấn MAS. Frontend chỉ gửi thông tin người dùng nhập;
/// Backend dựng context và hậu kiểm kết quả trước khi trả về.
/// </summary>
public record GetConsultationCommand(
    string DeviceType,
    string Brand,
    string Model,
    string IssueDescription,
    DateOnly? RequestedDate = null,
    bool AllowQuotation = true,
    bool AllowAppointment = true,
    string? FullName = null,
    string? Phone = null) : IRequest<ConsultationResponse>;
