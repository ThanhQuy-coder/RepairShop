using RepairShop.Application.Modules.AIAdvisory.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.AIAdvisory.Commands.GetAIAdvice;

// Khớp API Contract Task 4 mục 13.1 Tuần 2: POST /api/ai/advice
public record GetAIAdviceCommand(string DeviceType, string Brand, string Model, string IssueDescription)
    : IRequest<AIAdviceResponse>;