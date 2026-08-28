using System.Net.Http.Json;
using System.Text.Json;
using RepairShop.IntegrationTests.TestDoubles;

namespace RepairShop.IntegrationTests.Scenarios;

internal static class WorkflowHelpers
{
    /// <summary>Dựng sẵn 1 ticket tới bước "đã có Quote WAITING_APPROVAL" — dùng chung cho Scenario B/C/E.</summary>
    public static async Task<(Guid TicketId, Guid QuoteId)> CreateTicketUpToQuote(
        HttpClient client, string technicianToken, Guid technicianUserId,
        Guid? customerUserId = null)
    {
        var custRes = await client.PostAsJsonAsync("/api/customers", new
        {
            fullName = "Khach Test",
            phone = $"09{Random.Shared.Next(10000000, 99999999)}",
            email = (string?)null,
            address = (string?)null,
            userId = customerUserId // mới
        });
        var customerId = (await custRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        var devRes = await client.PostAsJsonAsync("/api/devices",
            new { customerId, deviceType = "Phone", brand = "Samsung", model = "S23", serialNumber = $"IMEI-{Guid.NewGuid():N}"[..15] });
        var deviceId = (await devRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        var ticketRes = await client.PostAsJsonAsync("/api/tickets",
            new { customerId, deviceId, issueDescription = "Màn hình bị sọc", notes = (string?)null, conditionNotes = (string?)null, riskWarning = (string?)null });
        var ticketId = (await ticketRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/assign-technician",
            new { technicianId = technicianUserId, note = (string?)null });

        client.AuthorizeAs(technicianToken);
        await client.PatchAsync($"/api/tickets/{ticketId}/start-diagnosis", null);
        await client.PatchAsJsonAsync($"/api/tickets/{ticketId}/diagnosis", new
        {
            diagnosisResult = "Màn hình LCD hỏng",
            rootCause = "Va đập",
            recommendedRepair = "Thay màn hình",
            requiredPartsNote = "Màn hình Samsung S23",
            technicalNote = (string?)null
        });

        var quoteRes = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/quotes", new
        {
            description = "Báo giá thay màn hình",
            items = new[] { new { itemType = "Service", description = "Công thay màn hình", quantity = 1, unitPrice = 200000m, partId = (Guid?)null } }
        });
        var quoteId = (await quoteRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        return (ticketId, quoteId);
    }
}