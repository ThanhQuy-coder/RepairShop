using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.AIAdvisory.Commands.GetAIAdvice;
using RepairShop.Application.Modules.AIAdvisory.DTOs;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace RepairShop.UnitTests.Application.AIAdvisory;

public class GetAIAdviceCommandHandlerTests
{
    [Fact]
    public async Task Handle_CallsIAIService_WithRequestBuiltFromCommand_NotFromDatabaseDirectly()
    {
        // Xác nhận Handler KHÔNG tự query catalog — trách nhiệm đó thuộc về AIServiceClient
        // (Task 6.15), Handler chỉ chuyển tiếp Command -> AIAdviceRequest -> gọi IAIService.
        var aiServiceMock = new Mock<IAIService>();
        aiServiceMock
            .Setup(s => s.GetAdviceAsync(It.IsAny<AIAdviceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIAdviceResult(AIAdvisoryStatus.NoMatch, [], [], null, "no match", null));

        var catalogMock = new Mock<IServiceCatalogQueryService>(); // Handler không dùng tới interface này

        var handler = new GetAIAdviceCommandHandler(aiServiceMock.Object, catalogMock.Object, NullLogger<GetAIAdviceCommandHandler>.Instance);

        var command = new GetAIAdviceCommand("Phone", "iPhone", "13", "Pin tụt nhanh");
        await handler.Handle(command, CancellationToken.None);

        aiServiceMock.Verify(
            s => s.GetAdviceAsync(
                It.Is<AIAdviceRequest>(r => r.DeviceType == "Phone" && r.Brand == "iPhone" && r.IssueDescription == "Pin tụt nhanh"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        catalogMock.VerifyNoOtherCalls(); // Handler không gọi trực tiếp catalog — đúng phân tầng
    }
}