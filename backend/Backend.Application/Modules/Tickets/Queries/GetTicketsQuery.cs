using RepairShop.Application.Modules.Tickets.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Tickets.Queries;

public record GetTicketsQuery(
    string? Status,
    Guid? TechnicianId,
    Guid? CustomerId,
    int Page = 1,
    int PageSize = 20) : IRequest<TicketListResponse>;