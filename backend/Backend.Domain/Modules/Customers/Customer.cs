using RepairShop.Domain.Common;
using RepairShop.Domain.Modules.Devices;
using RepairShop.Domain.Modules.Identity;
using RepairShop.Domain.Modules.Tickets;
using RepairShop.Domain.Common.Exceptions;


namespace RepairShop.Domain.Modules.Customers;

public class Customer : BaseEntity
{
    public Guid? UserId { get; private set; } // nullable — khách vãng lai không có tài khoản
    public string FullName { get; private set; } = default!;
    public string Phone { get; private set; } = default!;
    public string? Email { get; private set; }
    public string? Address { get; private set; }

    public User? User { get; private set; }
    public ICollection<Device> Devices { get; private set; } = new List<Device>();
    public ICollection<RepairTicket> RepairTickets { get; private set; } = new List<RepairTicket>();

    private Customer() { } // for EF Core

    public Customer(string fullName, string phone, string? email = null, string? address = null, Guid? userId = null)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new DomainException("Số điện thoại không được để trống.");

        FullName = fullName;
        Phone = phone;
        Email = email;
        Address = address;
        UserId = userId;
    }

    public void UpdateProfile(string fullName, string phone, string? email, string? address)
    {
        FullName = fullName;
        Phone = phone;
        Email = email;
        Address = address;
        MarkUpdated();
    }

    public void LinkUser(Guid userId)
    {
        if (UserId is not null && UserId != userId)
            throw new DomainException("Hồ sơ khách hàng đã liên kết với tài khoản khác.");

        UserId = userId;
        MarkUpdated();
    }
}