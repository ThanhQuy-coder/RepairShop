using Backend.Application.Common.Interfaces;
using Backend.Application.Modules.Customers.DTOs;
using MediatR;

namespace Backend.Application.Modules.Customers.Queries;

public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, CustomerResponse>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerByIdQueryHandler(ICustomerRepository customerRepository) =>
        _customerRepository = customerRepository;

    public async Task<CustomerResponse> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException($"Không tìm thấy khách hàng với Id '{request.Id}'.");

        return new CustomerResponse(customer.Id, customer.FullName, customer.Phone, customer.Email,
            customer.Address, customer.CreatedAt);
    }
}