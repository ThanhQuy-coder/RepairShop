using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.AIAdvisory.Commands.GetConsultation;
using RepairShop.Application.Modules.AIAdvisory.DTOs;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace RepairShop.UnitTests.Application.AIAdvisory;

public class GetConsultationCommandHandlerTests
{
    [Fact]
    public async Task Handle_MapsCommandToConsultationRequest_AndReturnsServiceResult()
    {
        var serviceMock = new Mock<IAIConsultationService>();
        serviceMock
            .Setup(s => s.GetConsultationAsync(
                It.IsAny<ConsultationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConsultationResponse(
                true, "SUCCESS", "DIAGNOSIS", ["DIAGNOSIS"],
                null, null, null, "ok", "disclaimer", null));

        var handler = new GetConsultationCommandHandler(
            serviceMock.Object,
            NullLogger<GetConsultationCommandHandler>.Instance);

        var command = new GetConsultationCommand(
            "Phone", "iPhone", "13", "Pin tụt nhanh",
            new DateOnly(2026, 11, 10), true, true, "Nguyễn Văn A", "0901234567");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.AiAvailable);
        Assert.Equal("SUCCESS", result.Status);

        serviceMock.Verify(
            s => s.GetConsultationAsync(
                It.Is<ConsultationRequest>(request =>
                    request.DeviceType == "Phone" &&
                    request.Brand == "iPhone" &&
                    request.Model == "13" &&
                    request.IssueDescription == "Pin tụt nhanh" &&
                    request.RequestedDate == new DateOnly(2026, 11, 10) &&
                    request.AllowQuotation &&
                    request.AllowAppointment &&
                    request.FullName == "Nguyễn Văn A" &&
                    request.Phone == "0901234567"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenServiceThrows_ReturnsUnavailableFallback_NotThrow()
    {
        var serviceMock = new Mock<IAIConsultationService>();
        serviceMock
            .Setup(s => s.GetConsultationAsync(
                It.IsAny<ConsultationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("AI down"));

        var handler = new GetConsultationCommandHandler(
            serviceMock.Object,
            NullLogger<GetConsultationCommandHandler>.Instance);

        var result = await handler.Handle(
            new GetConsultationCommand("Phone", "iPhone", "13", "Pin tụt nhanh"),
            CancellationToken.None);

        Assert.False(result.AiAvailable);
        Assert.Equal("ERROR", result.Status);
        Assert.Contains("không khả dụng", result.Message);
    }
}
