using System.Net.Http.Json;
using System.Text.Json;
using RepairShop.IntegrationTests.TestDoubles;

namespace RepairShop.IntegrationTests.Scenarios;

internal static class WorkflowHelpers
{
    /// <summary>Dựng sẵn 1 ticket tới bước "đã có Quote WAITING_APPROVAL" — dùng chung cho Scenario B/C/E.</summary>
    public static async Task<(Guid TicketId, Guid QuoteId)> CreateTicketUpToQuote(
        HttpClient client, string technicianToken, Guid technicianUserId,
        Guid? customerUserId = null, Guid? customerId = null)
    {
        var receptionistToken = client.DefaultRequestHeaders.Authorization?.Parameter
            ?? throw new InvalidOperationException("Workflow helper requires a receptionist token.");

        if (customerId is null)
        {
            var custRes = await client.PostAsJsonAsync("/api/customers", new
            {
                fullName = "Khach Test",
                phone = $"09{Random.Shared.Next(10000000, 99999999)}",
                email = (string?)null,
                address = (string?)null,
                userId = customerUserId
            });
            customerId = (await custRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();
        }

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

        client.AuthorizeAs(receptionistToken);
        var quoteRes = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/quotes", new
        {
            description = "Báo giá thay màn hình",
            items = new[] { new { itemType = "Service", quantity = 1, partId = (Guid?)null, serviceId = (Guid?)new Guid("00000000-0000-0000-0000-000000000102") } }
        });
        var quoteId = (await quoteRes.ReadAsAsync<JsonElement>()).GetProperty("id").GetGuid();

        return (ticketId, quoteId);
    }

    /// <summary>Hoàn tất đủ 3 checklist kiểm tra kỹ thuật (Functional, Appearance, RepairVerification) trước khi gọi qa-pass.</summary>
    public static async Task CompleteInspectionsForQaAsync(HttpClient technicianClient, Guid ticketId)
    {
        var funcTemplates = await (await technicianClient.GetAsync($"/api/tickets/{ticketId}/inspection-templates/Functional"))
            .ReadAsAsync<JsonElement>();
        var funcItems = funcTemplates.EnumerateArray().Select(t => new
        {
            templateId = (Guid?)t.GetProperty("id").GetGuid(),
            itemName = t.GetProperty("name").GetString()!,
            status = "Pass",
            note = (string?)null
        }).ToList();
        await technicianClient.PutAsJsonAsync($"/api/tickets/{ticketId}/inspections/Functional", new { items = funcItems });

        var appTemplates = await (await technicianClient.GetAsync($"/api/tickets/{ticketId}/inspection-templates/Appearance"))
            .ReadAsAsync<JsonElement>();
        var appItems = appTemplates.EnumerateArray().Select(t => new
        {
            templateId = (Guid?)t.GetProperty("id").GetGuid(),
            itemName = t.GetProperty("name").GetString()!,
            status = "Normal",
            note = (string?)null
        }).ToList();
        await technicianClient.PutAsJsonAsync($"/api/tickets/{ticketId}/inspections/Appearance", new { items = appItems });

        await technicianClient.PutAsJsonAsync($"/api/tickets/{ticketId}/inspections/RepairVerification", new
        {
            items = new[]
            {
                new
                {
                    templateId = (Guid?)null,
                    itemName = "Xác nhận khắc phục lỗi ban đầu",
                    status = "Fixed",
                    note = (string?)null
                }
            }
        });
    }
}
