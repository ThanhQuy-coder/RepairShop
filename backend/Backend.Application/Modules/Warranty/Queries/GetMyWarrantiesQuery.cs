using RepairShop.Application.Common.Interfaces;
using MediatR;

namespace RepairShop.Application.Modules.Warranty.Queries;

public record MyWarrantyItem(
    string WarrantyCode, Guid TicketId, string TicketCode, string DeviceLabel,
    DateOnly StartDate, DateOnly EndDate, string Status, bool IsExpired);

public record GetMyWarrantiesQuery : IRequest<List<MyWarrantyItem>>;

public class GetMyWarrantiesQueryHandler : IRequestHandler<GetMyWarrantiesQuery, List<MyWarrantyItem>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IWarrantyRepository _warrantyRepository; // mới, xem mục 4
    private readonly ICurrentUserService _currentUser;

    public GetMyWarrantiesQueryHandler(ICustomerRepository customerRepository,
        IWarrantyRepository warrantyRepository, ICurrentUserService currentUser)
    {
        _customerRepository = customerRepository;
        _warrantyRepository = warrantyRepository;
        _currentUser = currentUser;
    }

    public async Task<List<MyWarrantyItem>> Handle(GetMyWarrantiesQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByUserIdAsync(_currentUser.UserId!.Value);
        if (customer is null) return [];

        var warranties = await _warrantyRepository.GetByCustomerIdAsync(customer.Id);

        return warranties.Select(w => new MyWarrantyItem(
            w.Warranty.WarrantyCode, w.TicketId, w.TicketCode, w.DeviceLabel,
            w.Warranty.StartDate, w.Warranty.EndDate, w.Warranty.Status.ToString(), w.Warranty.IsExpired()))
            .ToList();
    }
}