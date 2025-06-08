using BackBuddy.Api.Service.V1.Notifications.Dtos;
using BackBuddy.Core.Library.Notifications;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BackBuddy.Api.Service.V1.Notifications.Services
{
    public class DevNotificationService(IHttpClientFactory httpClientFactory, IOptions<DevNotificationConfig> config, ILogger<DevNotificationService> logger) : INotificationService
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly DevNotificationConfig _config = config.Value;
        private readonly ILogger<DevNotificationService> _logger = logger;

        public async Task SendNotification(IEnumerable<string> tokens, Notification notification, CancellationToken cancellationToken = default)
        {
            try
            {
                using HttpClient httpClient = _httpClientFactory.CreateClient();

                NotificationDevDebugDto debugDto = new() { Notification = notification, Tokens = tokens };

                HttpResponseMessage response = await httpClient.PostAsJsonAsync(_config.Uri, debugDto, cancellationToken);

                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to send notification to dev endpoint! Notification: {Notification} Tokens: {Tokens}", JsonSerializer.Serialize(notification), tokens);
            }
        }
    }
}
