using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RepairShop.IntegrationTests.TestDoubles;

public static class HttpClientExtensions
{
    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static void AuthorizeAs(this HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    public static async Task<T> ReadAsAsync<T>(this HttpResponseMessage response)
    {
        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return result ?? throw new InvalidOperationException("Response body rỗng hoặc parse JSON thất bại.");
    }
}