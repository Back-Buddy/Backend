using System.Net.Http.Json;
using System.Text.Json.Nodes;

internal class NotificationLib
{
    private readonly HttpClient _httpClient;

    public NotificationLib(string baseUri)
    {
        _httpClient = new()
        {
            BaseAddress = new Uri(baseUri),
        };
    }

    public async Task<JsonArray> GetNotifications()
    {
        return await _httpClient.GetFromJsonAsync<JsonArray>("/cache");
    }

    public async Task ClearNotifications()
    {
        await _httpClient.DeleteAsync("/clear");
    }
}