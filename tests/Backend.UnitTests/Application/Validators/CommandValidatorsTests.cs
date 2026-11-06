using RepairShop.Application.Modules.Appointments.Commands;
using RepairShop.Application.Modules.Appointments.Validators;
using RepairShop.Application.Modules.Customers.Commands;
using RepairShop.Application.Modules.Customers.Validators;
using RepairShop.Application.Modules.Identity.Commands;
using RepairShop.Application.Modules.Identity.Validators;
using RepairShop.Application.Modules.Inventory.Commands;
using RepairShop.Application.Modules.Inventory.Validators;
using RepairShop.Application.Modules.Reviews.Commands;
using RepairShop.Application.Modules.Reviews.Validators;
using RepairShop.Application.Modules.Tickets.Commands;
using RepairShop.Application.Modules.Tickets.Queries;
using RepairShop.Application.Modules.Tickets.Validators;
using RepairShop.Application.Modules.Warranty.Commands;
using RepairShop.Application.Modules.Warranty.Validators;
using RepairShop.Domain.Modules.Appointments.Enums;

namespace RepairShop.UnitTests.Application.Validators;

public class CommandValidatorsTests
{
    [Theory]
    [InlineData("", "a@b.com", "123456", "0901234567", false)]
    [InlineData("Nguyen A", "not-an-email", "123456", "0901234567", false)]
    [InlineData("Nguyen A", "a@b.com", "123", "0901234567", false)]
    [InlineData("Nguyen A", "a@b.com", "123456", "", false)]
    [InlineData("Nguyen A", "a@b.com", "123456", "0901234567", true)]
    public void RegisterCommandValidator_ValidatesFields(
        string fullName, string email, string password, string phone, bool expectedValid)
    {
        var validator = new RegisterCommandValidator();
        var result = validator.Validate(new RegisterCommand(fullName, email, password, phone));
        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData("", "secret", false)]
    [InlineData("bad-email", "secret", false)]
    [InlineData("user@shop.vn", "", false)]
    [InlineData("user@shop.vn", "secret", true)]
    public void LoginCommandValidator_ValidatesFields(string email, string password, bool expectedValid)
    {
        var validator = new LoginCommandValidator();
        var result = validator.Validate(new LoginCommand(email, password));
        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact]
    public void CreateQuoteCommandValidator_EnforcesServiceAndPartLinkingRules()
    {
        var validator = new CreateQuoteCommandValidator();
        var ticketId = Guid.NewGuid();

        // Rỗng danh sách items -> invalid
        Assert.False(validator.Validate(new CreateQuoteCommand(ticketId, "Mô tả", [])).IsValid);

        // Service nhưng không có ServiceId -> invalid
        var missingServiceId = new CreateQuoteCommand(ticketId, "Mô tả",
            [new QuoteItemInput("Service", 1, null, null)]);
        Assert.False(validator.Validate(missingServiceId).IsValid);

        // Service nhưng truyền cả PartId -> invalid
        var bothIds = new CreateQuoteCommand(ticketId, "Mô tả",
            [new QuoteItemInput("Service", 1, Guid.NewGuid(), Guid.NewGuid())]);
        Assert.False(validator.Validate(bothIds).IsValid);

        // Quantity <= 0 -> invalid
        var zeroQty = new CreateQuoteCommand(ticketId, "Mô tả",
            [new QuoteItemInput("Part", 0, Guid.NewGuid(), null)]);
        Assert.False(validator.Validate(zeroQty).IsValid);

        // Hợp lệ
        var valid = new CreateQuoteCommand(ticketId, "Báo giá chuẩn",
        [
            new QuoteItemInput("Service", 1, null, Guid.NewGuid()),
            new QuoteItemInput("Part", 2, Guid.NewGuid(), null)
        ]);
        Assert.True(validator.Validate(valid).IsValid);
    }

    [Theory]
    [InlineData("RT-20261105-0001", true)]
    [InlineData("RT-20261105-12345", true)]
    [InlineData("", false)]
    [InlineData("RT-2026-0001", false)]
    [InlineData("INVALID-CODE", false)]
    public void TrackTicketByCodeQueryValidator_ValidatesTicketCodeFormat(string code, bool expectedValid)
    {
        var validator = new TrackTicketByCodeQueryValidator();
        Assert.Equal(expectedValid, validator.Validate(new TrackTicketByCodeQuery(code)).IsValid);
    }

    [Fact]
    public void CreatePartCommandValidator_ValidatesPricesAndDeviceType()
    {
        var validator = new CreatePartCommandValidator();

        Assert.True(validator.Validate(
            new CreatePartCommand("Pin", "SKU-1", 100, 200, "Cat", "Phone", "cái", 5)).IsValid);
        Assert.True(validator.Validate(
            new CreatePartCommand("Ốc vít", "SKU-2", 0, 0, null, null, "con", 0)).IsValid);
        Assert.False(validator.Validate(
            new CreatePartCommand("Pin", "SKU-1", -1, 200, null, "Phone", "cái", 0)).IsValid);
        Assert.False(validator.Validate(
            new CreatePartCommand("Pin", "SKU-1", 100, 200, null, "Smartwatch", "cái", 0)).IsValid);
    }

    [Fact]
    public void CreateAppointmentCommandValidator_EnforcesFutureDateAndWarrantyParentTicket()
    {
        var validator = new CreateAppointmentCommandValidator();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

        var pastAppt = new CreateAppointmentCommand(
            "Khach", "0901234567", yesterday, Guid.NewGuid(), null, null, null, null, null, null);
        Assert.False(validator.Validate(pastAppt).IsValid);

        var warrantyWithoutParent = new CreateAppointmentCommand(
            "Khach", "0901234567", tomorrow, Guid.NewGuid(), null,
            null, null, null, null, null, AppointmentType.WarrantyRevisit, ParentTicketId: null);
        Assert.False(validator.Validate(warrantyWithoutParent).IsValid);

        var validWarranty = warrantyWithoutParent with { ParentTicketId = Guid.NewGuid() };
        Assert.True(validator.Validate(validWarranty).IsValid);
    }

    [Fact]
    public void OtherValidators_EnforceCoreConstraints()
    {
        var reviewValidator = new CreateReviewCommandValidator();
        Assert.False(reviewValidator.Validate(new CreateReviewCommand(Guid.NewGuid(), 0, "bad")).IsValid);
        Assert.False(reviewValidator.Validate(new CreateReviewCommand(Guid.NewGuid(), 6, "bad")).IsValid);
        Assert.True(reviewValidator.Validate(new CreateReviewCommand(Guid.NewGuid(), 5, "good")).IsValid);

        var usePartValidator = new UsePartCommandValidator();
        Assert.False(usePartValidator.Validate(new UsePartCommand(Guid.NewGuid(), Guid.NewGuid(), 0)).IsValid);
        Assert.True(usePartValidator.Validate(new UsePartCommand(Guid.NewGuid(), Guid.NewGuid(), 1)).IsValid);

        var diagValidator = new SubmitDiagnosisCommandValidator();
        Assert.False(diagValidator.Validate(
            new SubmitDiagnosisCommand(Guid.NewGuid(), "", null, null, null, null)).IsValid);
        Assert.True(diagValidator.Validate(
            new SubmitDiagnosisCommand(Guid.NewGuid(), "Hỏng màn", null, null, null, null)).IsValid);

        var customerValidator = new CreateCustomerCommandValidator();
        Assert.False(customerValidator.Validate(
            new CreateCustomerCommand("A", "0901111111", "invalid-email", null, null)).IsValid);
        Assert.True(customerValidator.Validate(
            new CreateCustomerCommand("A", "0901111111", null, null, null)).IsValid);

        var ticketValidator = new CreateTicketCommandValidator();
        Assert.False(ticketValidator.Validate(
            new CreateTicketCommand(Guid.Empty, Guid.NewGuid(), "Lỗi", null, null, null)).IsValid);

        var claimValidator = new CreateWarrantyClaimCommandValidator();
        Assert.False(claimValidator.Validate(new CreateWarrantyClaimCommand(
            Guid.NewGuid(), "", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), Guid.NewGuid())).IsValid);
    }
}
