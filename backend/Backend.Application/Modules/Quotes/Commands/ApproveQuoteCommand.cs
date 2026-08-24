using RepairShop.Application.Modules.Tickets.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Quotes.Commands;

public record ApproveQuoteCommand(Guid QuoteId) : IRequest<QuoteResponse>;