using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Customers.DTOs;
using RepairShop.Domain.Modules.Customers;
using MediatR;
using Microsoft.Extensions.Logging;
using RepairShop.Domain.Common;

namespace RepairShop.Application.Modules.Customers.Commands;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, CustomerResponse>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<CreateCustomerCommandHandler> _logger;

    public CreateCustomerCommandHandler(ICustomerRepository customerRepository,
        ILogger<CreateCustomerCommandHandler> logger, IUserRepository userRepository)
    {
        _customerRepository = customerRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<CustomerResponse> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var existing = await _customerRepository.GetByPhoneAsync(request.Phone);
        if (existing is not null)
            throw new InvalidOperationException($"Khách hàng với số điện thoại '{request.Phone}' đã tồn tại.");

        if (request.UserId is not null)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId.Value)
                ?? throw new KeyNotFoundException($"Không tìm thấy tài khoản với Id '{request.UserId}'.");

            if (user.Role?.Name != Roles.Customer)
                throw new InvalidOperationException($"Tài khoản '{user.Email}' không có vai trò Customer.");

            var alreadyLinked = await _customerRepository.GetByUserIdAsync(request.UserId.Value);
            if (alreadyLinked is not null)
                throw new InvalidOperationException($"Tài khoản '{user.Email}' đã liên kết với 1 hồ sơ khách hàng khác.");
        }

        var customer = new Customer(request.FullName, request.Phone, 
            request.Email, request.Address, request.UserId);

        await _customerRepository.AddAsync(customer);
        await _customerRepository.SaveChangesAsync();

        _logger.LogInformation("Tạo mới Customer {CustomerId} - {Phone}, liên kết UserId: {UserId}",
            customer.Id, customer.Phone, request.UserId);

        return new CustomerResponse(customer.Id, customer.FullName, customer.Phone, customer.Email,
            customer.Address, customer.CreatedAt);
    }
}