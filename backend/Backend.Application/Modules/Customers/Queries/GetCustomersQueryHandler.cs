using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Customers.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Customers.Queries;

public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, CustomerListResponse>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomersQueryHandler(ICustomerRepository customerRepository) =>
        _customerRepository = customerRepository;

    public async Task<CustomerListResponse> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _customerRepository.SearchAsync(request.Search, request.Page, request.PageSize);

        var response = items.Select(c =>
            new CustomerResponse(c.Id, c.FullName, c.Phone, c.Email, c.Address, c.CreatedAt)).ToList();

        return new CustomerListResponse(response, total);
    }
}