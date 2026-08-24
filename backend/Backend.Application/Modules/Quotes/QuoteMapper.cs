using RepairShop.Application.Modules.Tickets.DTOs;
using RepairShop.Domain.Modules.Quotes;

namespace RepairShop.Application.Modules.Quotes;

internal static class QuoteMapper
{
    public static QuoteResponse ToResponse(Quote quote) => new(
        quote.Id, quote.RepairTicketId, quote.Description, quote.TotalAmount, quote.Status.ToString(),
        quote.Items.Select(i => new QuoteItemResponse(i.Id, i.ItemType.ToString(), i.Description,
            i.Quantity, i.UnitPrice, i.Subtotal)).ToList(),
        quote.CreatedAt);
}