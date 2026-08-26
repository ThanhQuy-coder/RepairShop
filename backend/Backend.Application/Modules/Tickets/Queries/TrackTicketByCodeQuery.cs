using RepairShop.Application.Modules.Tickets.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Tickets.Queries;

public record TrackTicketByCodeQuery(string TicketCode) : IRequest<PublicTicketTrackingResponse>;