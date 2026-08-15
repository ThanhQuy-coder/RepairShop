using RepairShop.Domain.Common;
using RepairShop.Domain.Modules.Customers;
using RepairShop.Domain.Common.Exceptions;

namespace RepairShop.Domain.Modules.Identity;

public class User : BaseEntity
{
    public string FullName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string? Phone { get; private set; }
    public int RoleId { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Role Role { get; private set; } = default!;
    public Customer? Customer { get; private set; }

    private User() { } // for EF Core

    public User(string fullName, string email, string passwordHash, int roleId, string? phone = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("Họ tên không được để trống.");

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email không được để trống.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("PasswordHash không được để trống.");

        FullName = fullName;
        Email = email;
        PasswordHash = passwordHash;
        RoleId = roleId;
        Phone = phone;
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }
}