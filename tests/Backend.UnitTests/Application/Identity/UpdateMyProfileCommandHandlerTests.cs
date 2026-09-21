using Moq;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Identity.Commands;
using RepairShop.Domain.Common;
using RepairShop.Domain.Modules.Identity;

namespace RepairShop.UnitTests.Application.Identity;

public class UpdateMyProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_UpdatesCurrentUserFullNameAndPhone()
    {
        var user = new User("Old Name", "user@example.com", "hash", 1, "0900000000");
        var userId = user.Id;

        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UserId).Returns(userId);

        var handler = new UpdateMyProfileCommandHandler(repo.Object, currentUser.Object);

        var result = await handler.Handle(new UpdateMyProfileCommand("New Name", "0911111111"), CancellationToken.None);

        Assert.Equal("New Name", result.FullName);
        Assert.Equal("0911111111", result.Phone);
        repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
