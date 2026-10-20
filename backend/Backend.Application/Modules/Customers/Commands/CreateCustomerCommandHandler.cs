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
    private readonly IAppointmentRepository _appointmentRepository;

    public CreateCustomerCommandHandler(ICustomerRepository customerRepository,
        ILogger<CreateCustomerCommandHandler> logger,
        IUserRepository userRepository,
        IAppointmentRepository appointmentRepository)
    {
        _customerRepository = customerRepository;
        _userRepository = userRepository;
        _logger = logger;
        _appointmentRepository = appointmentRepository;
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

        Guid? linkedUserId = request.UserId;
        string emailUser = string.Empty;
        if (linkedUserId is null)
        {
            var userWithSamePhone = await _userRepository.GetByPhoneAsync(request.Phone);
            if (userWithSamePhone is not null)
            {
                if (userWithSamePhone.Role?.Name != Roles.Customer)
                    throw new InvalidOperationException(
                        $"Số điện thoại '{request.Phone}' đang thuộc tài khoản không phải Customer.");

                var alreadyLinked = await _customerRepository.GetByUserIdAsync(userWithSamePhone.Id);
                if (alreadyLinked is not null)
                    throw new InvalidOperationException(
                        $"Tài khoản '{userWithSamePhone.Email}' đã liên kết với 1 hồ sơ khách hàng khác.");

                linkedUserId = userWithSamePhone.Id;
                emailUser = userWithSamePhone.Email;
            }
        }

        var customer = new Customer(request.FullName, request.Phone,
            emailUser, request.Address, linkedUserId);

        await _customerRepository.AddAsync(customer);
        await _customerRepository.SaveChangesAsync();

        var appointmentUpdated = await _appointmentRepository.UpdateCustomerIdAsync(request.Phone, customer.Id);

        if (appointmentUpdated > 0)
        {
            _logger.LogInformation("Cập nhật {Count} cuộc hẹn với số điện thoại {Phone} sang CustomerId: {CustomerId}",
                appointmentUpdated, request.Phone, customer.Id);
        }
        else
        {
            _logger.LogInformation("Không tìm thấy cuộc hẹn nào với số điện thoại {Phone} để cập nhật sang CustomerId: {CustomerId}",
                request.Phone, customer.Id);
        }

        return new CustomerResponse(customer.Id, customer.FullName, customer.Phone, customer.Email,
            customer.Address, customer.CreatedAt);
    }
}