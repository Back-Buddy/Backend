using BackBuddy.Api.Service.V1.Notifications.Dtos;
using FirebaseAdmin.Messaging;

namespace BackBuddy.Api.Service.V1.Notifications.Services
{
    public class DevNotificationService(IHttpClientFactory httpClientFactory, DevNotificationConfig config) : INotificationService
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly DevNotificationConfig _config = config;

        public async Task SendNotification(IEnumerable<string> tokens, Notification notification)
        {
            using HttpClient httpClient = _httpClientFactory.CreateClient();

            NotificationDevDebugDto debugDto = new() { Notification = notification, Tokens = tokens };

            HttpResponseMessage response = await httpClient.PostAsJsonAsync(_config.Uri, debugDto);

            response.EnsureSuccessStatusCode();
        }
    }
}
