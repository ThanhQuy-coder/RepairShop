using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Tickets.DTOs;

public record SubmitDiagnosisCommand(
    Guid TicketId,
    string DiagnosisResult,
    string? RootCause,
    string? RecommendedRepair,
    string? RequiredPartsNote,
    string? TechnicalNote) : IRequest<TicketResponse>;

public class SubmitDiagnosisCommandHandler : IRequestHandler<SubmitDiagnosisCommand, TicketResponse>
{
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly ICurrentUserService _currentUser;

    public SubmitDiagnosisCommandHandler(IRepairTicketRepository ticketRepository, ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _currentUser = currentUser;
    }

    public async Task<TicketResponse> Handle(SubmitDiagnosisCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        RepairShop.Application.Common.Authorization.TicketAccessGuard.EnsureCanAccess(ticket, _currentUser);

        // Không đổi trạng thái — chỉ ghi dữ liệu, nên KHÔNG tạo StatusHistory, không cần TrackNewX
        ticket.SubmitDiagnosis(request.DiagnosisResult, request.RootCause,
            request.RecommendedRepair, request.RequiredPartsNote);

        if (!string.IsNullOrWhiteSpace(request.TechnicalNote))
            ticket.AddNote(request.TechnicalNote);

        await _ticketRepository.SaveChangesAsync();

        return new TicketResponse(ticket.Id, ticket.TicketCode, ticket.CustomerId, ticket.DeviceId, 
            ticket.Status.Code, ticket.IssueReported, ticket.Notes, ticket.ConditionNotes, ticket.RiskWarning, ticket.ReceivedAt);
    }
}