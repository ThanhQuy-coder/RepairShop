using MediatR;
using Microsoft.Extensions.Logging;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.AIAdvisory.DTOs;

namespace RepairShop.Application.Modules.AIAdvisory.Commands.GetConsultation;

public class GetConsultationCommandHandler
    : IRequestHandler<GetConsultationCommand, ConsultationResponse>
{
    private readonly IAIConsultationService _consultationService;
    private readonly ILogger<GetConsultationCommandHandler> _logger;

    public GetConsultationCommandHandler(
        IAIConsultationService consultationService,
        ILogger<GetConsultationCommandHandler> logger)
    {
        _consultationService = consultationService;
        _logger = logger;
    }

    public async Task<ConsultationResponse> Handle(
        GetConsultationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
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
                    request.Phone),
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
