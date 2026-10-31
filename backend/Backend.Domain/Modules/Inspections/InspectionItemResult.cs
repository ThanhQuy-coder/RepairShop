namespace RepairShop.Domain.Modules.Inspections;

public class InspectionItemResult
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid InspectionResultId { get; private set; }
    public Guid? TemplateId { get; private set; }
    public string ItemNameSnapshot { get; private set; } = default!;
    public string Status { get; private set; } = default!;
    public string? Note { get; private set; }

    private InspectionItemResult() { }

    public InspectionItemResult(Guid inspectionResultId, Guid? templateId, string name, string status, string? note)
    {
        InspectionResultId = inspectionResultId;
        TemplateId = templateId;
        ItemNameSnapshot = name;
        Status = status;
        Note = note;
    }
}
