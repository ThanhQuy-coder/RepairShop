using MediatR;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Customers.DTOs;

namespace RepairShop.Application.Modules.Customers.Queries;

public record FindCustomerByPhoneQuery(string Phone) : IRequest<CustomerResponse?>;

public class FindCustomerByPhoneQueryHandler : 
    IRequestHandler<FindCustomerByPhoneQuery, CustomerResponse?>
{
    private readonly ICustomerRepository _customerRepository;

    public FindCustomerByPhoneQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerResponse?> Handle(FindCustomerByPhoneQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByPhoneAsync(request.Phone);
        if (customer is null) return null;

        return new CustomerResponse(
            customer.Id,
            customer.FullName,
            customer.Phone,
            customer.Email,
            customer.Address,
            customer.CreatedAt
        );
    }
}