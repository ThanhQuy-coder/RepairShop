using MediatR;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Quotes;
using RepairShop.Application.Modules.Tickets;
using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Modules.Quotes;
using RepairShop.Domain.Modules.Quotes.Enums;
using RepairShop.Domain.Modules.Content;
using RepairShop.Domain.Modules.Inventory;

public class CreateQuoteCommandHandler : IRequestHandler<CreateQuoteCommand, QuoteResponse>
{
    private static ValidationException Invalid(string message) =>
        new(new[] { new ValidationFailure("Items", message) });
    private readonly IRepairTicketRepository _ticketRepository;
    private readonly IQuoteRepository _quoteRepository;
    private readonly IRepairStatusRepository _statusRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ICustomerRepository _customerRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly IPartRepository _partRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CreateQuoteCommandHandler> _logger;

    public CreateQuoteCommandHandler(IRepairTicketRepository ticketRepository,
    IQuoteRepository quoteRepository,
        IRepairStatusRepository statusRepository,
        ICurrentUserService currentUser,
        ICustomerRepository customerRepository,
        IServiceRepository serviceRepository,
        IPartRepository partRepository,
        INotificationService notificationService,
        ILogger<CreateQuoteCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _quoteRepository = quoteRepository;
        _statusRepository = statusRepository;
        _currentUser = currentUser;
        _customerRepository = customerRepository;
        _serviceRepository = serviceRepository;
        _partRepository = partRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Thực hiện tạo mới 1 Quote cho RepairTicket, kèm các QuoteItem bên trong.
    /// Nếu là Requote (báo giá trước đó bị từ chối), sẽ đánh dấu Quote cũ là NeedsRequote.
    /// Nếu là Quote đầu tiên, sẽ chuyển RepairTicket sang trạng thái WaitingApproval.
    /// </summary>
    public async Task<QuoteResponse> Handle(CreateQuoteCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId)
            ?? throw new NotFoundException("Phiếu sửa chữa", request.TicketId);

        var userId = _currentUser.UserId!.Value;

        var previous = ticket.Quotes.OrderByDescending(q => q.Version).FirstOrDefault();
        var isRequote = previous?.Status is QuoteStatus.QuoteRejected
            or QuoteStatus.NeedsRequote;

        if (isRequote)
            previous!.MarkNeedsRequote();

        var quote = new Quote(ticket.Id, request.Description, userId,
            (previous?.Version ?? 0) + 1, previous?.Id);

        foreach (var item in request.Items)
        {
            if (!Enum.TryParse<QuoteItemType>(item.ItemType, true, out var itemType))
                throw Invalid("ItemType phải là Service hoặc Part.");

            if (itemType == QuoteItemType.Service)
            {
                if (item.ServiceId is null || item.PartId is not null)
                    throw Invalid("Hạng mục dịch vụ phải có ServiceId và không có PartId.");

                var service = await _serviceRepository.GetByIdAsync(item.ServiceId.Value);
                if (service is null || service.IsDeleted)
                    throw new NotFoundException("Dịch vụ", item.ServiceId.Value);
                if (!service.IsActive)
                    throw Invalid($"Dịch vụ '{service.Name}' đã ngừng hoạt động.");
                if (!string.IsNullOrWhiteSpace(service.DeviceType) &&
                    !string.Equals(service.DeviceType, ticket.Device.DeviceType.ToString(), StringComparison.OrdinalIgnoreCase))
                    throw Invalid($"Dịch vụ '{service.Name}' không phù hợp với loại thiết bị.");
                if (service.BasePrice is null)
                    throw Invalid($"Dịch vụ '{service.Name}' chưa có giá niêm yết.");

                quote.AddItem(itemType, service.Name, item.Quantity, service.BasePrice.Value,
                    serviceId: service.Id);
            }
            else
            {
                if (item.PartId is null || item.ServiceId is not null)
                    throw Invalid("Hạng mục linh kiện phải có PartId và không có ServiceId.");

                var part = await _partRepository.GetByIdAsync(item.PartId.Value);
                if (part is null || part.IsDeleted)
                    throw new NotFoundException("Linh kiện", item.PartId.Value);
                if (!part.IsActive)
                    throw Invalid($"Linh kiện '{part.Name}' đã ngừng hoạt động.");

                quote.AddItem(itemType, part.Name, item.Quantity, part.UnitPrice, partId: part.Id);
            }
        }

        ticket.AttachQuote(quote);

        var waitingApprovalStatus = await _statusRepository.GetByCodeAsync(RepairStatusCodes.WaitingApproval);
        if (!isRequote)
            ticket.SubmitQuote(waitingApprovalStatus, userId); // DIAGNOSING -> WAITING_APPROVAL

        await _quoteRepository.AddAsync(quote);

        _ticketRepository.TrackNewStatusHistory(ticket.StatusHistories.Last());

        await _quoteRepository.SaveChangesAsync();

        await TicketNotificationHelper.NotifyCustomerIfLinkedAsync(
            _customerRepository, _notificationService, ticket.CustomerId,
            NotificationTypes.QuoteCreated, "Báo giá mới cần xác nhận",
            $"Phiếu {ticket.TicketCode} đã có báo giá {quote.TotalAmount:N0}đ, vui lòng xem và phản hồi.",
            ticket.Id);

        _logger.LogInformation("Tạo Quote {QuoteId} cho Ticket {TicketCode}, tổng tiền {TotalAmount}",
            quote.Id, ticket.TicketCode, quote.TotalAmount);

        return QuoteMapper.ToResponse(quote);
    }
}