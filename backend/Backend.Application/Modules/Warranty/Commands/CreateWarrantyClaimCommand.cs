using MediatR;
using RepairShop.Application.Modules.Tickets.DTOs;

namespace RepairShop.Application.Modules.Warranty.Commands;

// Khớp API Spec Tuần 2: POST /api/tickets/{id}/warranty-claim { issueReported }
public record CreateWarrantyClaimCommand(Guid ParentTicketId, string IssueReported) : IRequest<TicketResponse>;