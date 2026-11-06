using Moq;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Identity.Commands;
using RepairShop.Application.Modules.Inspections;
using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Common.Exceptions;
using RepairShop.Domain.Modules.Billing;
using RepairShop.Domain.Modules.Billing.Enums;
using RepairShop.Domain.Modules.Customers;
using RepairShop.Domain.Modules.Identity;
using RepairShop.Domain.Modules.Inspections;
using RepairShop.Domain.Modules.Quotes;
using RepairShop.Domain.Modules.Quotes.Enums;
using RepairShop.Domain.Modules.Tickets;
using Xunit;

namespace RepairShop.UnitTests.Application.Handlers;

public class CoreModuleHandlersTests
{
    private static RepairStatus Status(int id, string code) => new(id, code, code, id);

    private static RepairTicket CreateReadyForPickupTicketWithQuote(Guid staffId, Guid techId, bool approveQuote)
    {
        var ticket = new RepairTicket("REP-20260410-0010", Guid.NewGuid(), Guid.NewGuid(), staffId,
            "Lỗi sạc", Status(1, RepairStatusCodes.CheckedIn));
        ticket.AssignTechnician(techId, Status(2, RepairStatusCodes.Assigned), staffId);
        ticket.StartDiagnosis(Status(3, RepairStatusCodes.Diagnosing), techId);
        ticket.SubmitDiagnosis("Hỏng chân sạc", "Oxy hóa", "Thay cụm sạc", null);

        var quote = new Quote(ticket.Id, "Báo giá cụm sạc", staffId);
        quote.AddItem(QuoteItemType.Service, "Thay cụm sạc", 1, 450000m, serviceId: Guid.NewGuid());
        if (approveQuote) quote.Approve();
        ticket.AttachQuote(quote);

        ticket.SubmitQuote(Status(4, RepairStatusCodes.WaitingApproval), staffId);
        ticket.ApproveQuote(Status(5, RepairStatusCodes.InRepair), staffId);
        ticket.RecordCompletionNotes("Đã thay cụm sạc");
        ticket.StartQualityCheck(Status(6, RepairStatusCodes.QaTesting), techId);
        ticket.PassQualityCheck(Status(7, RepairStatusCodes.ReadyForPickup), techId);
        return ticket;
    }

    [Fact]
    public async Task Login_WhenValidCredentials_ShouldReturnAuthResponse()
    {
        var userRepo = new Mock<IUserRepository>();
        var hasher = new Mock<IPasswordHasher>();
        var jwt = new Mock<IJwtTokenGenerator>();

        var user = new User("Admin", "admin@test.com", "hashed-pw", 4, "0901234567");
        userRepo.Setup(r => r.GetByEmailAsync("admin@test.com")).ReturnsAsync(user);
        hasher.Setup(h => h.Verify("Pass@123", "hashed-pw")).Returns(true);
        jwt.Setup(j => j.GenerateAccessToken(user, "Customer", "0901234567")).Returns("jwt-token-123");

        var handler = new LoginCommandHandler(userRepo.Object, hasher.Object, jwt.Object);
        var result = await handler.Handle(new LoginCommand("admin@test.com", "Pass@123"), CancellationToken.None);

        Assert.Equal("jwt-token-123", result.AccessToken);
        Assert.Equal("admin@test.com", result.Email);
        Assert.Equal(user.Id, result.userId);
    }

    [Fact]
    public async Task Login_WhenWrongPasswordOrInactive_ShouldThrowInvalidCredentialsException()
    {
        var userRepo = new Mock<IUserRepository>();
        var hasher = new Mock<IPasswordHasher>();
        var jwt = new Mock<IJwtTokenGenerator>();

        var user = new User("User", "user@test.com", "hashed-pw", 4, "0901234567");
        userRepo.Setup(r => r.GetByEmailAsync("user@test.com")).ReturnsAsync(user);
        hasher.Setup(h => h.Verify("wrong", "hashed-pw")).Returns(false);

        var handler = new LoginCommandHandler(userRepo.Object, hasher.Object, jwt.Object);
        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => handler.Handle(new LoginCommand("user@test.com", "wrong"), CancellationToken.None));

        // Inactive user with correct password
        user.Deactivate();
        hasher.Setup(h => h.Verify("Pass@123", "hashed-pw")).Returns(true);
        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => handler.Handle(new LoginCommand("user@test.com", "Pass@123"), CancellationToken.None));
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyExists_ShouldThrowEmailAlreadyExistsException()
    {
        var userRepo = new Mock<IUserRepository>();
        var roleRepo = new Mock<IRoleRepository>();
        var hasher = new Mock<IPasswordHasher>();
        var jwt = new Mock<IJwtTokenGenerator>();
        var custRepo = new Mock<ICustomerRepository>();

        var existing = new User("Exist", "dup@test.com", "hash", 4, "0901112223");
        userRepo.Setup(r => r.GetByEmailAsync("dup@test.com")).ReturnsAsync(existing);

        var handler = new RegisterCommandHandler(userRepo.Object, roleRepo.Object, hasher.Object, jwt.Object, custRepo.Object);
        await Assert.ThrowsAsync<EmailAlreadyExistsException>(
            () => handler.Handle(new RegisterCommand("New", "dup@test.com", "Pass@123", "0901112223"), CancellationToken.None));
    }

    [Fact]
    public async Task Register_WhenMatchingWalkInCustomerByPhone_ShouldAutoLinkCustomerAndReturnToken()
    {
        var userRepo = new Mock<IUserRepository>();
        var roleRepo = new Mock<IRoleRepository>();
        var hasher = new Mock<IPasswordHasher>();
        var jwt = new Mock<IJwtTokenGenerator>();
        var custRepo = new Mock<ICustomerRepository>();

        var role = new Role(4, Roles.Customer, "Khách hàng");
        var walkInCustomer = new Customer("Khach WalkIn", "0909998887", null, null, null);

        userRepo.Setup(r => r.GetByEmailAsync("new@test.com")).ReturnsAsync((User?)null);
        roleRepo.Setup(r => r.GetByNameAsync(Roles.Customer)).ReturnsAsync(role);
        hasher.Setup(h => h.Hash("Pass@123")).Returns("hashed-pw");
        custRepo.Setup(c => c.GetByPhoneAsync("0909998887")).ReturnsAsync(walkInCustomer);
        jwt.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), Roles.Customer, "0909998887")).Returns("new-jwt");

        var handler = new RegisterCommandHandler(userRepo.Object, roleRepo.Object, hasher.Object, jwt.Object, custRepo.Object);
        var res = await handler.Handle(new RegisterCommand("Khach WalkIn", "new@test.com", "Pass@123", "0909998887"), CancellationToken.None);

        Assert.Equal("new-jwt", res.AccessToken);
        Assert.Equal(res.userId, walkInCustomer.UserId);
        custRepo.Verify(c => c.Update(walkInCustomer), Times.Once);
    }

    [Fact]
    public async Task CreateInvoice_ShouldCreateFromApprovedQuote_AndRejectWhenNoApprovedQuote()
    {
        var ticketRepo = new Mock<IRepairTicketRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var staffId = Guid.NewGuid();
        var techId = Guid.NewGuid();
        currentUser.Setup(u => u.UserId).Returns(staffId);

        var unapprovedTicket = CreateReadyForPickupTicketWithQuote(staffId, techId, approveQuote: false);
        ticketRepo.Setup(r => r.GetByIdAsync(unapprovedTicket.Id)).ReturnsAsync(unapprovedTicket);
        var handler = new CreateInvoiceCommandHandler(ticketRepo.Object, currentUser.Object);

        // Chưa có quote được duyệt -> DomainException
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new CreateInvoiceCommand(unapprovedTicket.Id, "Cash"), CancellationToken.None));
        Assert.Contains("chưa có báo giá được duyệt", ex.Message);

        // Có quote được duyệt -> tạo Invoice đúng tổng tiền
        var approvedTicket = CreateReadyForPickupTicketWithQuote(staffId, techId, approveQuote: true);
        ticketRepo.Setup(r => r.GetByIdAsync(approvedTicket.Id)).ReturnsAsync(approvedTicket);

        var invoiceRes = await handler.Handle(new CreateInvoiceCommand(approvedTicket.Id, "Transfer"), CancellationToken.None);
        Assert.Equal(450000m, invoiceRes.TotalAmount);
        Assert.Equal("Transfer", invoiceRes.PaymentMethod);
        ticketRepo.Verify(r => r.TrackNewInvoice(It.IsAny<Invoice>()), Times.Once);
    }

    [Fact]
    public async Task MarkInvoicePaid_ShouldMarkPaid_AndThrowNotFoundWhenMissing()
    {
        var invoiceRepo = new Mock<IInvoiceRepository>();
        var staffId = Guid.NewGuid();
        var techId = Guid.NewGuid();
        var ticket = CreateReadyForPickupTicketWithQuote(staffId, techId, approveQuote: true);
        var invoice = ticket.CreateInvoice(PaymentMethod.Cash, 450000m, staffId);
        invoiceRepo.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);

        var handler = new MarkInvoicePaidCommandHandler(invoiceRepo.Object);
        var res = await handler.Handle(new MarkInvoicePaidCommand(invoice.Id, null), CancellationToken.None);

        Assert.NotNull(res.PaidAt);
        Assert.NotNull(invoice.PaidAt);
        invoiceRepo.Verify(r => r.SaveChangesAsync(), Times.Once);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new MarkInvoicePaidCommand(Guid.NewGuid(), null), CancellationToken.None));
    }

    [Fact]
    public async Task SaveInspection_ShouldValidateRequiredNotes_AndSaveValidChecklist()
    {
        var ticketRepo = new Mock<IRepairTicketRepository>();
        var inspectionRepo = new Mock<IInspectionRepository>();
        var currentUser = new Mock<ICurrentUserService>();

        var techId = Guid.NewGuid();
        currentUser.Setup(u => u.UserId).Returns(techId);
        currentUser.Setup(u => u.Role).Returns(Roles.Technician);

        var ticket = new RepairTicket("REP-20260410-0020", Guid.NewGuid(), Guid.NewGuid(), techId,
            "Lỗi loa", Status(1, RepairStatusCodes.CheckedIn));
        ticket.AssignTechnician(techId, Status(2, RepairStatusCodes.Assigned), techId);
        ticketRepo.Setup(r => r.GetByIdAsync(ticket.Id)).ReturnsAsync(ticket);

        var handler = new SaveInspectionCommandHandler(ticketRepo.Object, inspectionRepo.Object, currentUser.Object);

        // NotFixed mà không có Note -> ValidationException
        var invalidCmd = new SaveInspectionCommand(ticket.Id, InspectionType.RepairVerification,
            [new InspectionItemInput(null, "Kiểm tra loa", "NotFixed", null)]);
        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(invalidCmd, CancellationToken.None));

        // Hợp lệ -> lưu thành công
        var validCmd = new SaveInspectionCommand(ticket.Id, InspectionType.RepairVerification,
            [new InspectionItemInput(null, "Kiểm tra loa", "Fixed", "Âm thanh rõ")]);
        var validRes = await handler.Handle(validCmd, CancellationToken.None);

        Assert.Single(validRes.Items);
        Assert.Equal("Kiểm tra loa", validRes.Items[0].ItemName);
        Assert.Equal("Fixed", validRes.Items[0].Status);
        inspectionRepo.Verify(r => r.AddResultAsync(It.IsAny<InspectionResult>()), Times.Once);
        inspectionRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
