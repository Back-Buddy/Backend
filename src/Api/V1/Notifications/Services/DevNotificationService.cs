using BackBuddy.Api.Service.V1.Notifications.Dtos;
using BackBuddy.Core.Library.Notifications;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Options;

namespace BackBuddy.Api.Service.V1.Notifications.Services
{
    public class DevNotificationService(IHttpClientFactory httpClientFactory, IOptions<DevNotificationConfig> config) : INotificationService
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly DevNotificationConfig _config = config.Value;

        public async Task SendNotification(IEnumerable<string> tokens, Notification notification, CancellationToken cancellationToken = default)
        {
            using HttpClient httpClient = _httpClientFactory.CreateClient();

            NotificationDevDebugDto debugDto = new() { Notification = notification, Tokens = tokens };

            HttpResponseMessage response = await httpClient.PostAsJsonAsync(_config.Uri, debugDto, cancellationToken);

            response.EnsureSuccessStatusCode();
        }
    }
}
