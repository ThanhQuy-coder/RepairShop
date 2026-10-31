using RepairShop.Domain.Modules.Inspections;

namespace RepairShop.Application.Modules.Inspections;

public sealed record InspectionTemplateResponse(Guid Id, string Name, string? DeviceType, string InspectionType, int SortOrder);
public sealed record InspectionItemResponse(Guid Id, Guid? TemplateId, string ItemName, string Status, string? Note);
public sealed record InspectionResponse(Guid TicketId, string InspectionType, List<InspectionItemResponse> Items);

public sealed record InspectionItemInput(Guid? TemplateId, string ItemName, string Status, string? Note);
public sealed record SaveInspectionCommand(Guid TicketId, InspectionType InspectionType,
    List<InspectionItemInput> Items) : MediatR.IRequest<InspectionResponse>;
public sealed record GetInspectionQuery(Guid TicketId, InspectionType InspectionType)
    : MediatR.IRequest<InspectionResponse>;
public sealed record GetInspectionTemplatesQuery(Guid TicketId, InspectionType InspectionType)
    : MediatR.IRequest<List<InspectionTemplateResponse>>;
