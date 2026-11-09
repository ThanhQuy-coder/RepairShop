using System.Net;
using System.Net.Http.Json;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.AIAdvisory.DTOs;
using RepairShop.Domain.Modules.Appointments;
using RepairShop.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace RepairShop.UnitTests.Infrastructure.AI;

public class AIConsultationClientTests
{
    private static readonly DateOnly Tomorrow = new(2026, 11, 10);

    private static AIConsultationClient CreateClient(
        HttpResponseMessage response,
        List<CatalogServiceItem>? services = null,
        List<CatalogPartItem>? parts = null,
        List<TimeSlotConfig>? slots = null,
        int booked = 0)
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(response))
        {
            BaseAddress = new Uri("http://localhost:8000")
        };

        var catalog = new Mock<IServiceCatalogQueryService>();
        catalog.Setup(c => c.GetAvailableServicesAsync(It.IsAny<string>()))
            .ReturnsAsync(services ?? []);
        catalog.Setup(c => c.GetAvailablePartsAsync(It.IsAny<string?>()))
            .ReturnsAsync(parts ?? []);

        var slotRepository = new Mock<ITimeSlotConfigRepository>();
        slotRepository.Setup(r => r.ListAsync())
            .ReturnsAsync(slots ?? []);

        var appointmentRepository = new Mock<IAppointmentRepository>();
        appointmentRepository
            .Setup(r => r.CountBookedAsync(It.IsAny<DateOnly>(), It.IsAny<Guid>()))
            .ReturnsAsync(booked);

        var clock = new Mock<IBusinessClock>();
        clock.SetupGet(c => c.Now)
            .Returns(new DateTimeOffset(2026, 11, 9, 8, 0, 0, TimeSpan.FromHours(7)));

        return new AIConsultationClient(
            httpClient,
            catalog.Object,
            slotRepository.Object,
            appointmentRepository.Object,
            clock.Object,
            NullLogger<AIConsultationClient>.Instance);
    }

    private static TimeSlotConfig CreateSlot()
        => new(new TimeOnly(9, 0), new TimeOnly(10, 0), maxCapacity: 3);

    private static object SuccessBody(TimeSlotConfig slot, string? serviceId = "svc-1")
    {
        return new
        {
            requestId = Guid.NewGuid(),
            status = "SUCCESS",
            intent = "DIAGNOSIS",
            executedAgents = new[] { "DIAGNOSIS", "QUOTATION", "APPOINTMENT" },
            diagnosis = new
            {
                possibleCauses = new[] { new { name = "Pin chai", confidence = "HIGH" } },
                suggestedServices = new[]
                {
                    new { serviceId, serviceName = "TÊN DO AI BỊA", confidence = "HIGH" }
                },
                suggestedParts = new[]
                {
                    new { partId = "part-1", partName = "TÊN DO AI BỊA", confidence = "MEDIUM" }
                },
                missingInformation = Array.Empty<string>(),
                explanation = "Pin có thể đã chai."
            },
            quotation = new
            {
                items = new[]
                {
                    new
                    {
                        itemId = serviceId, itemType = "SERVICE", name = "TÊN DO AI BỊA",
                        quantity = 1, unitPrice = 999_999m, lineTotal = 999_999m
                    }
                },
                estimatedTotal = 999_999m,
                currency = "VND",
                assumptions = Array.Empty<string>(),
                explanation = "Giá tham khảo."
            },
            appointment = new
            {
                suggestedSlots = new[]
                {
                    new
                    {
                        timeSlotId = slot.Id.ToString(), slotStart = "09:00", slotEnd = "10:00",
                        available = 3, isFull = false, appointmentDate = "2026-11-10"
                    }
                },
                bookingDraft = new
                {
                    fullName = (string?)null, phone = (string?)null,
                    appointmentDate = "2026-11-10", timeSlotId = slot.Id.ToString(),
                    deviceType = "phone", brand = "iPhone", model = "13",
                    issueDescription = "Pin tụt nhanh"
                },
                missingInformation = new[] { "fullName", "phone" },
                requiresUserConfirmation = true,
                explanation = "Còn chỗ."
            },
            reason = "Đã tư vấn.",
            disclaimer = "Tham khảo."
        };
    }

    [Fact]
    public async Task GetConsultationAsync_UsesCatalogNamesAndPrices_NotAiText()
    {
        var slot = CreateSlot();
        var services = new List<CatalogServiceItem>
        {
            new("svc-1", "Thay pin", "Phone", 400_000m)
        };
        var parts = new List<CatalogPartItem>
        {
            new("part-1", "Pin iPhone 13", 350_000m, "Phone", true, 10)
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(SuccessBody(slot))
        };
        var client = CreateClient(response, services, parts, [slot]);

        var result = await client.GetConsultationAsync(new ConsultationRequest(
            "Phone", "iPhone", "13", "Pin tụt nhanh", Tomorrow));

        Assert.True(result.AiAvailable);
        Assert.Equal("SUCCESS", result.Status);
        Assert.NotNull(result.Diagnosis);
        Assert.Equal("Thay pin", result.Diagnosis!.SuggestedServices.Single().ServiceName);
        Assert.Equal("Pin iPhone 13", result.Diagnosis.SuggestedParts.Single().PartName);

        Assert.NotNull(result.Quotation);
        Assert.Equal(400_000m, result.Quotation!.Items.Single().UnitPrice);
        Assert.Equal(400_000m, result.Quotation.EstimatedTotal);

        Assert.NotNull(result.Appointment);
        Assert.Equal(slot.Id.ToString(), result.Appointment!.SuggestedSlots.Single().TimeSlotId);
        Assert.Equal(Tomorrow, result.Appointment.BookingDraft!.AppointmentDate);
        Assert.True(result.Appointment.RequiresUserConfirmation);
    }

    [Fact]
    public async Task GetConsultationAsync_DropsUnknownIdsAndInvalidSlot()
    {
        var slot = CreateSlot();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(SuccessBody(slot, serviceId: "svc-fake"))
        };
        var client = CreateClient(response, [], [], [slot]);

        var result = await client.GetConsultationAsync(new ConsultationRequest(
            "Phone", "iPhone", "13", "Pin tụt nhanh", Tomorrow));

        Assert.NotNull(result.Diagnosis);
        Assert.Empty(result.Diagnosis!.SuggestedServices);
        Assert.Empty(result.Diagnosis.SuggestedParts); // part-1 không có trong catalog
        Assert.NotNull(result.Quotation);
        Assert.Empty(result.Quotation!.Items);
        Assert.NotNull(result.Appointment);
        Assert.Equal(slot.Id.ToString(), result.Appointment!.SuggestedSlots.Single().TimeSlotId);
    }

    [Fact]
    public async Task GetConsultationAsync_WhenAiFails_ReturnsUnavailable()
    {
        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        var client = CreateClient(response, [new("svc-1", "Thay pin", "Phone", 100m)]);

        var result = await client.GetConsultationAsync(new ConsultationRequest(
            "Phone", "iPhone", "13", "Pin tụt nhanh", Tomorrow));

        Assert.False(result.AiAvailable);
        Assert.Equal("ERROR", result.Status);
        Assert.Contains("không khả dụng", result.Message);
    }

    [Fact]
    public async Task GetConsultationAsync_PastRequestedDate_DoesNotCallAi()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(SuccessBody(CreateSlot()))
        };
        var client = CreateClient(response);

        var result = await client.GetConsultationAsync(new ConsultationRequest(
            "Phone", "iPhone", "13", "Pin tụt nhanh", new DateOnly(2026, 11, 1)));

        Assert.Equal("NO_MATCH", result.Status);
        Assert.Contains("đã qua", result.Message);
    }

    [Fact]
    public async Task GetConsultationAsync_MapsOutOfScope()
    {
        var body = new
        {
            status = "OUT_OF_SCOPE",
            intent = "UNKNOWN",
            executedAgents = Array.Empty<string>(),
            reason = "Ngoài phạm vi.",
            disclaimer = "Tham khảo."
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(body)
        };
        var client = CreateClient(response);

        var result = await client.GetConsultationAsync(new ConsultationRequest(
            "Phone", "iPhone", "13", "Viết bài thơ", Tomorrow));

        Assert.True(result.AiAvailable);
        Assert.Equal("OUT_OF_SCOPE", result.Status);
    }
}
