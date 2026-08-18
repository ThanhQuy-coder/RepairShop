using RepairShop.Domain.Modules.Tickets.Enums;

namespace RepairShop.Domain.Modules.Tickets;

public class TicketImage
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid RepairTicketId { get; private set; }
    public string ImageUrl { get; private set; } = default!;
    public ImageType ImageType { get; private set; }
    public DateTime UploadedAt { get; private set; } = DateTime.UtcNow;

    private TicketImage() { } // for EF Core

    internal TicketImage(Guid repairTicketId, string imageUrl, ImageType imageType)
    {
        RepairTicketId = repairTicketId;
        ImageUrl = imageUrl;
        ImageType = imageType;
    }
}