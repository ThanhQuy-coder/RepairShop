using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Exceptions;

namespace RepairShop.Domain.Modules.Reviews;

public class Review : BaseEntity
{
    public Guid RepairTicketId { get; private set; }
    public Guid CustomerId { get; private set; }
    public int Rating { get; private set; }
    public string? Comment { get; private set; }
    public bool IsVisible { get; private set; } = true;

    private Review() { } // for EF Core

    public Review(Guid repairTicketId, Guid customerId, int rating, string? comment)
    {
        if (rating is < 1 or > 5)
            throw new DomainException("Đánh giá phải từ 1 đến 5 sao.");

        RepairTicketId = repairTicketId;
        CustomerId = customerId;
        Rating = rating;
        Comment = comment;
    }

    public void Hide() { IsVisible = false; MarkUpdated(); }
    public void Show() { IsVisible = true; MarkUpdated(); }
}