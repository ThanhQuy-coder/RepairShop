using RepairShop.Domain.Modules.Customers;
using RepairShop.Domain.Modules.Devices;
using RepairShop.Domain.Modules.Identity;
using RepairShop.Domain.Modules.Quotes;
using RepairShop.Domain.Modules.Tickets;
using RepairShop.Domain.Modules.Warranty;
using Microsoft.EntityFrameworkCore;

namespace RepairShop.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<RepairStatus> RepairStatuses => Set<RepairStatus>();
    public DbSet<RepairTicket> RepairTickets => Set<RepairTicket>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<Warranty> Warranties => Set<Warranty>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}