using MediatR;
using RepairShop.Application.Common.Authorization;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Common;
using RepairShop.Domain.Modules.Inspections;

namespace RepairShop.Application.Modules.Inspections;

internal static class InspectionMapper
{
    public static InspectionResponse ToResponse(Guid ticketId, InspectionType type, IEnumerable<InspectionItemResult> items) =>
        new(ticketId, type.ToString(), items.Select(x =>
            new InspectionItemResponse(x.Id, x.TemplateId, x.ItemNameSnapshot, x.Status, x.Note)).ToList());
}

public sealed class GetInspectionQueryHandler : IRequestHandler<GetInspectionQuery, InspectionResponse>
{
    private readonly IRepairTicketRepository _tickets;
    private readonly IInspectionRepository _inspections;
    private readonly ICurrentUserService _currentUser;
    private readonly ICustomerRepository _customers;

    public GetInspectionQueryHandler(IRepairTicketRepository tickets, IInspectionRepository inspections,
        ICurrentUserService currentUser, ICustomerRepository customers)
    {
        _tickets = tickets;
        _inspections = inspections;
        _currentUser = currentUser;
        _customers = customers;
    }

    public async Task<InspectionResponse> Handle(GetInspectionQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _tickets.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);
        if (_currentUser.Role == Roles.Customer)
        {
            var customer = await _customers.GetByUserIdAsync(_currentUser.UserId!.Value);
            TicketAccessGuard.EnsureCustomerOwnsTicket(ticket, customer);
        }
        else
        {
            TicketAccessGuard.EnsureCanAccess(ticket, _currentUser);
        }
        var result = await _inspections.GetResultAsync(request.TicketId, request.InspectionType);
        return InspectionMapper.ToResponse(request.TicketId, request.InspectionType, result?.Items ?? []);
    }

    public sealed class GetInspectionTemplatesQueryHandler
        : IRequestHandler<GetInspectionTemplatesQuery, List<InspectionTemplateResponse>>
    {
        private readonly IRepairTicketRepository _tickets;
        private readonly IInspectionRepository _inspections;
        private readonly ICurrentUserService _currentUser;

        public GetInspectionTemplatesQueryHandler(IRepairTicketRepository tickets, IInspectionRepository inspections,
            ICurrentUserService currentUser)
        {
            _tickets = tickets;
            _inspections = inspections;
            _currentUser = currentUser;
        }

        public async Task<List<InspectionTemplateResponse>> Handle(
            GetInspectionTemplatesQuery request, CancellationToken cancellationToken)
        {
            var ticket = await _tickets.GetByIdAsync(request.TicketId)
                ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);
            TicketAccessGuard.EnsureCanAccess(ticket, _currentUser);
            var templates = await _inspections.GetTemplatesAsync(ticket.Device.DeviceType, request.InspectionType);
            return templates.Select(x => new InspectionTemplateResponse(x.Id, x.Name,
                x.DeviceType?.ToString(), x.InspectionType.ToString(), x.SortOrder)).ToList();
        }
    }
}

public sealed class SaveInspectionCommandHandler : IRequestHandler<SaveInspectionCommand, InspectionResponse>
{
    private readonly IRepairTicketRepository _tickets;
    private readonly IInspectionRepository _inspections;
    private readonly ICurrentUserService _currentUser;

    public SaveInspectionCommandHandler(IRepairTicketRepository tickets, IInspectionRepository inspections,
        ICurrentUserService currentUser)
    {
        _tickets = tickets;
        _inspections = inspections;
        _currentUser = currentUser;
    }

    public async Task<InspectionResponse> Handle(SaveInspectionCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _tickets.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);
        TicketAccessGuard.EnsureCanAccess(ticket, _currentUser);

        var templates = request.InspectionType == InspectionType.RepairVerification
            ? [] : await _inspections.GetTemplatesAsync(ticket.Device.DeviceType, request.InspectionType);
        var templateMap = templates.ToDictionary(x => x.Id);
        ValidateItems(request, templateMap);

        var result = await _inspections.GetResultAsync(request.TicketId, request.InspectionType);
        if (result is null)
        {
            result = new InspectionResult(request.TicketId, request.InspectionType, _currentUser.UserId!.Value);
            await _inspections.AddResultAsync(result);
        }

        var items = request.Items.Select(item =>
        {
            var name = request.InspectionType == InspectionType.RepairVerification
                ? item.ItemName
                : templateMap[item.TemplateId!.Value].Name;
            return new InspectionItemResult(result.Id, item.TemplateId, name, item.Status, item.Note);
        });
        result.ReplaceItems(items);
        await _inspections.SaveChangesAsync();
        return InspectionMapper.ToResponse(request.TicketId, request.InspectionType, result.Items);
    }

    private static void ValidateItems(SaveInspectionCommand request,
        IReadOnlyDictionary<Guid, Domain.Modules.Inspections.InspectionTemplate> templates)
    {
        foreach (var item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Status))
                throw new ValidationException([new FluentValidation.Results.ValidationFailure("Items", "Trạng thái kiểm tra không được để trống.")]);

            var needsNote = request.InspectionType switch
            {
                InspectionType.Functional => item.Status is "Fail" or "NotTested",
                InspectionType.Appearance => item.Status is "Damaged" or "Unknown",
                InspectionType.RepairVerification => item.Status is "PartiallyFixed" or "NotFixed" or "UnableToVerify",
                _ => false
            };
            if (needsNote && string.IsNullOrWhiteSpace(item.Note))
                throw new ValidationException([new FluentValidation.Results.ValidationFailure("Items", $"Mục '{item.ItemName}' cần ghi chú.")]);

            if (request.InspectionType != InspectionType.RepairVerification &&
                (item.TemplateId is null || !templates.ContainsKey(item.TemplateId.Value)))
                throw new ValidationException([new FluentValidation.Results.ValidationFailure("Items", "Tiêu chí checklist không hợp lệ.")]);
        }
    }
}
