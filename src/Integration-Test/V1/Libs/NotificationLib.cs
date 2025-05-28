using BackBuddy.Integration_Test.Exceptions;
using BackBuddy.Integration_Test.Extensions;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
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

    public async Task SetFcmToken(string accessToken, string token)
    {
        JsonObject request = new()
            {
                { "FCMToken", token }
            };
        StringContent content = new(request.ToJsonString(), Encoding.UTF8, MediaTypeNames.Application.Json);

        HttpRequestMessage requestMessage = new(HttpMethod.Post, "/api/v1/notification");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        requestMessage.Content = content;

        HttpResponseMessage responseMessage = await _httpClient.SendAsync(requestMessage);
        if (!responseMessage.IsSuccessStatusCode)
            throw new RequestFailedException(responseMessage);
    }
}