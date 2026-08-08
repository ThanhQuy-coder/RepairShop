using Backend.Domain.Common;
using Backend.Domain.Modules.Devices;
using Backend.Domain.Modules.Identity;
using Backend.Domain.Modules.Tickets;

namespace Backend.Domain.Modules.Customers;

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
}