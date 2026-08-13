using Backend.Application.Common.Interfaces;
using Backend.Application.Modules.Customers.DTOs;
using Backend.Domain.Modules.Customers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Backend.Application.Modules.Customers.Commands;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, CustomerResponse>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<CreateCustomerCommandHandler> _logger;

    public CreateCustomerCommandHandler(ICustomerRepository customerRepository,
        ILogger<CreateCustomerCommandHandler> logger)
    {
        _customerRepository = customerRepository;
        _logger = logger;
    }

    public async Task<CustomerResponse> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var existing = await _customerRepository.GetByPhoneAsync(request.Phone);
        if (existing is not null)
            throw new InvalidOperationException($"Khách hàng với số điện thoại '{request.Phone}' đã tồn tại.");

        var customer = new Customer(request.FullName, request.Phone, request.Email, request.Address);

        await _customerRepository.AddAsync(customer);
        await _customerRepository.SaveChangesAsync();

        _logger.LogInformation("Tạo mới Customer {CustomerId} - {Phone}", customer.Id, customer.Phone);

        return new CustomerResponse(customer.Id, customer.FullName, customer.Phone, customer.Email,
            customer.Address, customer.CreatedAt);
    }
}