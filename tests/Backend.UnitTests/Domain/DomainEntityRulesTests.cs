using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Common.Exceptions;
using RepairShop.Domain.Modules.Billing.Enums;
using RepairShop.Domain.Modules.Identity;
using RepairShop.Domain.Modules.Inventory;
using RepairShop.Domain.Modules.Inventory.Enums;
using RepairShop.Domain.Modules.Quotes;
using RepairShop.Domain.Modules.Quotes.Enums;
using RepairShop.Domain.Modules.Reviews;
using RepairShop.Domain.Modules.Tickets;
using RepairShop.Domain.Modules.Tickets.Enums;
using RepairShop.Domain.Modules.Warranty;
using RepairShop.Domain.Modules.Warranty.Enums;

namespace RepairShop.UnitTests.Domain;

public class DomainEntityRulesTests
{
    private static RepairStatus Status(int id, string code) => new(id, code, code, id);

    private static RepairTicket CreateCheckedInTicket()
        => new("RT-20261105-0001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Màn hình vỡ, pin yếu", Status(1, RepairStatusCodes.CheckedIn));

    [Fact]
    public void User_Constructor_And_Lifecycle_WorkAsExpected()
    {
        Assert.Throws<DomainException>(() => new User("", "a@b.com", "hash", 1));
        Assert.Throws<DomainException>(() => new User("Name", "", "hash", 1));
        Assert.Throws<DomainException>(() => new User("Name", "a@b.com", "", 1));

        var user = new User("Nguyen Van A", "a@b.com", "hash1", 4, "0901234567");
        Assert.True(user.IsActive);

        user.UpdateProfile("  Tran Van B  ", "  0988888888  ");
        Assert.Equal("Tran Van B", user.FullName);
        Assert.Equal("0988888888", user.Phone);

        user.UpdateProfile("Tran Van B", "   ");
        Assert.Null(user.Phone);

        user.Deactivate();
        Assert.False(user.IsActive);
        user.Activate();
        Assert.True(user.IsActive);
    }

    [Fact]
    public void RefreshToken_IsActive_And_Revoke_WorkAsExpected()
    {
        var activeToken = new RefreshToken(Guid.NewGuid(), "hash-1", DateTime.UtcNow.AddDays(1));
        Assert.True(activeToken.IsActive);

        activeToken.Revoke();
        Assert.False(activeToken.IsActive);
        Assert.NotNull(activeToken.RevokedAt);

        var expiredToken = new RefreshToken(Guid.NewGuid(), "hash-2", DateTime.UtcNow.AddMinutes(-1));
        Assert.False(expiredToken.IsActive);
    }

    [Fact]
    public void Part_And_Inventory_Transactions_EnforceStockAndPriceRules()
    {
        Assert.Throws<DomainException>(() => new Part("", "SKU-1", 100, 200));
        Assert.Throws<DomainException>(() => new Part("Pin", "", 100, 200));
        Assert.Throws<DomainException>(() => new Part("Pin", "SKU-1", -1, 200));
        Assert.Throws<DomainException>(() => new Part("Pin", "SKU-1", 100, -1));

        var part = new Part("Pin iPhone", "SKU-IP13", 200_000m, 350_000m, minStockThreshold: 3);
        var inventory = new Inventory(part.Id);
        var staffId = Guid.NewGuid();

        Assert.True(inventory.IsLowStock(part.MinStockThreshold));

        var importTx = inventory.RecordImport(5, staffId);
        Assert.Equal(5, inventory.QuantityOnHand);
        Assert.Equal(TransactionType.Import, importTx.Type);
        Assert.False(inventory.IsLowStock(part.MinStockThreshold));

        inventory.RecordAdjustment(2, isIncrease: false, staffId);
        Assert.Equal(3, inventory.QuantityOnHand);

        Assert.Throws<InsufficientStockException>(() =>
            inventory.RecordAdjustment(10, isIncrease: false, staffId));

        var exportTx = inventory.RecordAutoExport(2, Guid.NewGuid(), staffId);
        Assert.NotNull(exportTx);
        Assert.Equal(1, inventory.QuantityOnHand);

        var failedExport = inventory.RecordAutoExport(5, Guid.NewGuid(), staffId);
        Assert.Null(failedExport);
        Assert.Equal(1, inventory.QuantityOnHand);
    }

    [Fact]
    public void Quote_CalculatesTotal_And_EnforcesStatusTransitions()
    {
        var ticketId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var quote = new Quote(ticketId, "Báo giá sửa chữa", userId);

        Assert.Throws<DomainException>(() => quote.Approve()); // chưa có item

        quote.AddItem(QuoteItemType.Service, "Công thay pin", 1, 150_000m, serviceId: Guid.NewGuid());
        quote.AddItem(QuoteItemType.Part, "Pin chính hãng", 2, 300_000m, partId: Guid.NewGuid());

        Assert.Equal(750_000m, quote.TotalAmount);
        Assert.Equal(QuoteStatus.Pending, quote.Status);

        quote.Reject("Khách chê đắt");
        Assert.Equal(QuoteStatus.QuoteRejected, quote.Status);
        Assert.Throws<DomainException>(() =>
            quote.AddItem(QuoteItemType.Service, "Thêm mục", 1, 50_000m));
        Assert.Throws<DomainException>(() => quote.Approve());

        quote.MarkNeedsRequote();
        Assert.Equal(QuoteStatus.NeedsRequote, quote.Status);
    }

    [Fact]
    public void RepairTicket_FullDomainLifecycle_EnforcesAllBusinessGuards()
    {
        var ticket = CreateCheckedInTicket();
        var userId = Guid.NewGuid();
        var techId = Guid.NewGuid();

        ticket.RecordInitialStatusHistory(userId);
        Assert.Throws<DomainException>(() => ticket.RecordInitialStatusHistory(userId));

        ticket.RecordIntakeCondition("Trầy viền", "Có thể mất chống nước");
        ticket.AddImage("https://cdn.local/before.jpg", ImageType.BeforeRepair, userId);
        Assert.Throws<DomainException>(() =>
            ticket.AddImage("https://cdn.local/after.jpg", ImageType.AfterRepair, userId));

        // Assign -> Diagnosing -> WaitingApproval
        ticket.AssignTechnician(techId, Status(2, RepairStatusCodes.Assigned), userId);
        Assert.Throws<DomainException>(() => ticket.RecordIntakeCondition("Sửa lại", null));

        ticket.StartDiagnosis(Status(3, RepairStatusCodes.Diagnosing), techId);
        Assert.Throws<DomainException>(() =>
            ticket.SubmitQuote(Status(4, RepairStatusCodes.WaitingApproval), userId)); // chưa có DiagnosisResult

        ticket.SubmitDiagnosis("Hỏng IC nguồn", "Chập điện", "Thay IC", "IC nguồn");
        ticket.SubmitQuote(Status(4, RepairStatusCodes.WaitingApproval), userId);
        ticket.ApproveQuote(Status(5, RepairStatusCodes.InRepair), userId);

        // InRepair: use part, completion notes, QA
        var part = new Part("IC nguồn", "SKU-IC", 100_000m, 250_000m);
        var inv = new Inventory(part.Id);
        inv.Add(1);
        ticket.UsePart(part, inv, 1, techId);
        Assert.Single(ticket.TicketParts);
        Assert.Equal(100_000m, ticket.TicketParts.First().CostPriceAtUse);

        ticket.RecordCompletionNotes("Đã thay IC và kiểm tra ổn định");
        ticket.StartQualityCheck(Status(6, RepairStatusCodes.QaTesting), techId);
        ticket.PassQualityCheck(Status(7, RepairStatusCodes.ReadyForPickup), techId);

        // Invoice & Payment gate before Deliver
        Assert.Throws<DomainException>(() =>
            ticket.Deliver(Status(8, RepairStatusCodes.Delivered), userId));

        var invoice = ticket.CreateInvoice(PaymentMethod.Cash, 250_000m, userId);
        Assert.Throws<DomainException>(() =>
            ticket.CreateInvoice(PaymentMethod.Cash, 250_000m, userId)); // BR-09: tối đa 1 invoice
        Assert.Throws<DomainException>(() =>
            ticket.Deliver(Status(8, RepairStatusCodes.Delivered), userId)); // chưa thanh toán

        invoice.MarkAsPaid();
        Assert.Throws<DomainException>(() => invoice.MarkAsPaid()); // không thanh toán 2 lần

        ticket.Deliver(Status(8, RepairStatusCodes.Delivered), userId);
        Assert.NotNull(ticket.DeliveredAt);

        // Warranty & Review after Delivered
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var warranty = ticket.CreateWarranty("WR-001", today, today.AddMonths(6), "Bảo hành 6 tháng");
        Assert.Throws<DomainException>(() =>
            ticket.CreateWarranty("WR-002", today, today.AddMonths(6), "Duplicate"));
        ticket.EnsureCanCreateWarrantyClaim();

        warranty.Void("Khách tự ý tháo máy");
        Assert.Equal(WarrantyStatus.Voided, warranty.Status);
        Assert.Throws<DomainException>(() => ticket.EnsureCanCreateWarrantyClaim());

        var review = ticket.CreateReview(5, "Rất hài lòng");
        Assert.Equal(5, review.Rating);
        Assert.Throws<DomainException>(() => ticket.CreateReview(4, "Đánh giá lần 2"));
    }

    [Fact]
    public void WarrantyPackage_And_Review_ValidateInputs()
    {
        Assert.Throws<DomainException>(() => new WarrantyPackage("", "desc", 6, "terms"));
        Assert.Throws<DomainException>(() => new WarrantyPackage("Gói 6T", "desc", 0, "terms"));
        Assert.Throws<DomainException>(() => new WarrantyPackage("Gói 6T", "desc", 6, ""));

        var pkg = new WarrantyPackage("  Gói tiêu chuẩn  ", "  Mô tả  ", 6, "  Điều khoản  ");
        Assert.Equal("Gói tiêu chuẩn", pkg.Name);
        pkg.SetActive(false);
        Assert.False(pkg.IsActive);

        Assert.Throws<DomainException>(() => new Review(Guid.NewGuid(), Guid.NewGuid(), 0, null));
        Assert.Throws<DomainException>(() => new Review(Guid.NewGuid(), Guid.NewGuid(), 6, null));
    }
}
