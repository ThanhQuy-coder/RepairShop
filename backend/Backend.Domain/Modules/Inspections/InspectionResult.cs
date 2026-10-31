using RepairShop.Domain.Common;

namespace RepairShop.Domain.Modules.Inspections;

public class InspectionResult : BaseEntity
{
    public Guid TicketId { get; private set; }
    public InspectionType InspectionType { get; private set; }
    public Guid CreatedBy { get; private set; }
    public ICollection<InspectionItemResult> Items { get; private set; } = new List<InspectionItemResult>();

    private InspectionResult() { }

    public InspectionResult(Guid ticketId, InspectionType inspectionType, Guid createdBy)
    {
        TicketId = ticketId;
        InspectionType = inspectionType;
        CreatedBy = createdBy;
    }

    public void ReplaceItems(IEnumerable<InspectionItemResult> items)
    {
        Items.Clear();
        foreach (var item in items)
            Items.Add(item);
        MarkUpdated();
    }
}
