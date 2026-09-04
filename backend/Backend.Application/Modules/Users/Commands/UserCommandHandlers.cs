using FluentValidation;
using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Users.DTOs;
using RepairShop.Domain.Common;
using RepairShop.Domain.Modules.Identity;

namespace RepairShop.Application.Modules.Users.Commands;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.Role).Must(role => role is Roles.Receptionist or Roles.Technician or Roles.Customer)
            .WithMessage("Vai trò không hợp lệ.");
    }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserListItemResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserListItemResponse> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.GetByEmailAsync(request.Email) is not null)
            throw new EmailAlreadyExistsException(request.Email);

        if (!string.IsNullOrWhiteSpace(request.Phone) &&
            await _userRepository.GetByPhoneAsync(request.Phone) is not null)
            throw new InvalidOperationException("Số điện thoại đã được sử dụng.");

        var role = await _roleRepository.GetByNameAsync(request.Role)
            ?? throw new NotFoundException("Vai trò", request.Role);
        var user = new User(request.FullName, request.Email,
            _passwordHasher.Hash(request.Password), role.Id, request.Phone);

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();
        return new UserListItemResponse(user.Id, user.FullName, user.Email, role.Name, user.IsActive);
    }
}

public class SetUserStatusCommandHandler : IRequestHandler<SetUserStatusCommand, UserListItemResponse>
{
    private readonly IUserRepository _userRepository;

    public SetUserStatusCommandHandler(IUserRepository userRepository) => _userRepository = userRepository;

    public async Task<UserListItemResponse> Handle(SetUserStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Người dùng", request.Id);

        if (request.IsActive) user.Activate();
        else user.Deactivate();
        await _userRepository.SaveChangesAsync();
        return new UserListItemResponse(user.Id, user.FullName, user.Email, user.Role.Name, user.IsActive);
    }
}
