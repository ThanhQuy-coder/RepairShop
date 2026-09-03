using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Identity.DTOs;
using RepairShop.Domain.Common;
using RepairShop.Domain.Modules.Identity;
using MediatR;

namespace RepairShop.Application.Modules.Identity.Commands;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ICustomerRepository _customerRepository;

    public RegisterCommandHandler(IUserRepository userRepository, IRoleRepository roleRepository,
        IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator,
        ICustomerRepository customerRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _customerRepository = customerRepository;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Lỗi FORMAT (email rỗng, sai định dạng...) đã bị chặn ở Validation Pipeline trước khi tới đây.
        // Ở đây chỉ còn lỗi NGHIỆP VỤ (email đã tồn tại) — 2 loại lỗi này tách biệt có chủ đích.
        var existing = await _userRepository.GetByEmailAsync(request.Email);
        if (existing is not null)
            throw new EmailAlreadyExistsException(request.Email);

        var customerRole = await _roleRepository.GetByNameAsync(Roles.Customer)
            ?? throw new InvalidOperationException("Role 'Customer' chưa được seed trong database.");

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = new User(request.FullName, request.Email, passwordHash, customerRole.Id, request.Phone);

        await _userRepository.AddAsync(user);

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var customer = await _customerRepository.GetByPhoneAsync(request.Phone);
            if (customer is not null)
            {
                if (customer.UserId is not null)
                    throw new InvalidOperationException(
                        "Số điện thoại này đã được liên kết với một tài khoản Customer khác.");

                customer.LinkUser(user.Id);
                _customerRepository.Update(customer);
            }
        }

        await _userRepository.SaveChangesAsync();

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, customerRole.Name);
        return new AuthResponse(accessToken, 3600, customerRole.Name, user.Email);
    }
}