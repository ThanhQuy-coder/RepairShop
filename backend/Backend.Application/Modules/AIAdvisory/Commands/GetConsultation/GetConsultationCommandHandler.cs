using MediatR;
using Microsoft.Extensions.Logging;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.AIAdvisory.DTOs;

namespace RepairShop.Application.Modules.AIAdvisory.Commands.GetConsultation;

public class GetConsultationCommandHandler
    : IRequestHandler<GetConsultationCommand, ConsultationResponse>
{
    private readonly IAIConsultationService _consultationService;
    private readonly ICurrentUserService? _currentUser;
    private readonly ILogger<GetConsultationCommandHandler> _logger;

    public GetConsultationCommandHandler(
        IAIConsultationService consultationService,
        ILogger<GetConsultationCommandHandler> logger,
        ICurrentUserService? currentUser = null)
    {
        _consultationService = consultationService;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<ConsultationResponse> Handle(
        GetConsultationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorType = _currentUser?.UserId is not null ? "CUSTOMER" : "GUEST";
            var phone = string.IsNullOrWhiteSpace(request.Phone)
                ? _currentUser?.Phone
                : request.Phone;

            return await _consultationService.GetConsultationAsync(
                new ConsultationRequest(
                    request.DeviceType,
                    request.Brand,
                    request.Model,
                    request.IssueDescription,
                    request.RequestedDate,
                    request.AllowQuotation,
                    request.AllowAppointment,
                    request.FullName,
                    phone,
                    actorType),
                cancellationToken);
        }
        catch (Exception ex)
        {
            // AI là tính năng hỗ trợ: không để lỗi AI làm gãy luồng người dùng.
            _logger.LogWarning(
                ex,
                "Tư vấn AI thất bại cho {DeviceType} {Brand} {Model}",
                request.DeviceType,
                request.Brand,
                request.Model);

            return ConsultationResponse.Unavailable(
                "Tính năng tư vấn AI tạm thời không khả dụng, vui lòng thử lại sau.");
        }
    }
}
