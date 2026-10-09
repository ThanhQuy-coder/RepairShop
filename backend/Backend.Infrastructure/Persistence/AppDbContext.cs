using RepairShop.Domain.Modules.Customers;
using RepairShop.Domain.Modules.Devices;
using RepairShop.Domain.Modules.Identity;
using RepairShop.Domain.Modules.Quotes;
using RepairShop.Domain.Modules.Tickets;
using RepairShop.Domain.Modules.Warranty;
using RepairShop.Domain.Modules.Inventory;
using Microsoft.EntityFrameworkCore;
using RepairShop.Domain.Modules.Billing;
using RepairShop.Domain.Modules.Content;
using RepairShop.Domain.Modules.Reviews;
using RepairShop.Domain.Modules.Appointments;
using RepairShop.Domain.Modules.Notifications;
using RepairShop.Domain.Modules.SLA;
using RepairShop.Domain.Common.Enums;

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
    public DbSet<RepairTicketStatusHistory> RepairTicketStatusHistories => Set<RepairTicketStatusHistory>();
    public DbSet<TicketImage> TicketImages => Set<TicketImage>();
    public DbSet<TicketPart> TicketParts => Set<TicketPart>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<QuoteItem> QuoteItems => Set<QuoteItem>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<TimeSlotConfig> TimeSlotConfigs => Set<TimeSlotConfig>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<SLAPolicy> SLAPolicies => Set<SLAPolicy>();
    public DbSet<TicketSLA> TicketSLAs => Set<TicketSLA>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.UsePropertyAccessMode(PropertyAccessMode.Field);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await SynchronizeTicketSlaLifecycleAsync(cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task SynchronizeTicketSlaLifecycleAsync(CancellationToken cancellationToken)
    {
        var ticketEntries = ChangeTracker.Entries<RepairTicket>()
            .Where(entry => entry.State == EntityState.Added ||
                (entry.State == EntityState.Modified &&
                 entry.Property(ticket => ticket.StatusId).IsModified))
            .ToList();

        foreach (var entry in ticketEntries)
        {
            var ticket = entry.Entity;
            var statusCode = ticket.Status?.Code
                ?? await RepairStatuses.Where(status => status.Id == ticket.StatusId)
                    .Select(status => status.Code)
                    .SingleAsync(cancellationToken);
            var deviceType = ticket.Device?.DeviceType
                ?? await Devices.Where(device => device.Id == ticket.DeviceId)
                    .Select(device => device.DeviceType)
                    .SingleAsync(cancellationToken);

            if (entry.State == EntityState.Modified)
            {
                var activeSlas = await TicketSLAs
                    .Where(sla => sla.RepairTicketId == ticket.Id &&
                        sla.Status != SLAStatus.Completed)
                    .ToListAsync(cancellationToken);
                foreach (var activeSla in activeSlas)
                    activeSla.Complete(DateTime.UtcNow);
            }

            var policy = await SLAPolicies
                .Where(candidate => candidate.IsActive &&
                    candidate.StatusCode == statusCode &&
                    (candidate.DeviceType == deviceType || candidate.DeviceType == null))
                .OrderByDescending(candidate => candidate.DeviceType.HasValue)
                .FirstOrDefaultAsync(cancellationToken);

            if (policy is not null)
                TicketSLAs.Add(new TicketSLA(ticket.Id, policy.Id, statusCode,
                    entry.State == EntityState.Added ? ticket.ReceivedAt : DateTime.UtcNow,
                    policy.DurationMinutes));
        }
    }
}