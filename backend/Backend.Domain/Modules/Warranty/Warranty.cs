using Backend.Domain.Common;
using Backend.Domain.Modules.Tickets;
using Backend.Domain.Common.Exceptions;

namespace Backend.Domain.Modules.Warranty;

public class Warranty : BaseEntity
{
    public Guid RepairTicketId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public string? Terms { get; private set; }

    public RepairTicket RepairTicket { get; private set; } = default!;

    private Warranty() { } // for EF Core

    public Warranty(Guid repairTicketId, DateOnly startDate, DateOnly endDate, string? terms = null)
    {
        if (endDate <= startDate)
            throw new DomainException(
                "Ngày kết thúc bảo hành phải sau ngày bắt đầu.");
                
        RepairTicketId = repairTicketId;
        StartDate = startDate;
        EndDate = endDate;
        Terms = terms;
    }

    public bool IsExpired() => DateOnly.FromDateTime(DateTime.UtcNow) > EndDate;
}