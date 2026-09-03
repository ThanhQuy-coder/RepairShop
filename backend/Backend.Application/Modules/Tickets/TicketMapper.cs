using RepairShop.Application.Modules.Tickets.DTOs;
using RepairShop.Domain.Modules.Tickets;

namespace RepairShop.Application.Modules.Tickets;

internal static class TicketMapper
{
    public static TicketResponse ToResponse(RepairTicket ticket) => new(
        ticket.Id, ticket.TicketCode, ticket.CustomerId, ticket.DeviceId, ticket.Status.Code,
        ticket.IssueReported, ticket.Notes, ticket.ConditionNotes, ticket.RiskWarning, ticket.ReceivedAt,
        ticket.Customer?.FullName,
        ticket.Customer?.Phone,
        ticket.Device?.DeviceType.ToString(),
        ticket.Device?.Brand,
        ticket.Device?.Model,
        ticket.Device?.SerialNumber,
        ticket.DiagnosisResult,
        ticket.RootCause,
        ticket.RecommendedRepair,
        ticket.RequiredPartsNote,
        ticket.CompletionNotes,
        ticket.Images?.Select(i => new TicketImageResponse(
            i.Id, i.ImageUrl, i.ImageType.ToString(), i.UploadedAt)).ToList(),
        ticket.TicketParts?.Select(tp => new TicketPartResponse(
            tp.Id, tp.Part?.Name ?? tp.PartId.ToString(), tp.Quantity,
            tp.UnitPriceAtUse, tp.Subtotal)).ToList(),
        ticket.Invoice is null
            ? null
            : new TicketInvoiceResponse(ticket.Invoice.Id, ticket.Invoice.TotalAmount,
                ticket.Invoice.PaymentMethod.ToString(), ticket.Invoice.PaidAt, ticket.Invoice.CreatedAt));
}