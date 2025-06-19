using BackBuddy.Core.Library.Database.Firebase;
using BackBuddy.Core.Library.Notifications;
using System.Text.Json;

namespace BackBuddy.Notification.Service.Services
{
    public class DevNotificationService(IHttpClientFactory httpClientFactory, FirebaseDevConfig config, ILogger<DevNotificationService> logger) : INotificationService
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly FirebaseDevConfig _config = config;
        private readonly ILogger<DevNotificationService> _logger = logger;

        public async Task SendNotification(IEnumerable<string> tokens, FirebaseAdmin.Messaging.Notification notification, CancellationToken cancellationToken = default)
        {
            try
            {
                using HttpClient httpClient = _httpClientFactory.CreateClient();

                NotificationDevDebugDto debugDto = new() { Notification = notification, Tokens = tokens };

                HttpResponseMessage response = await httpClient.PostAsJsonAsync(_config.NotificationEmulatorHost, debugDto, cancellationToken);

                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to send notification to dev endpoint! Notification: {Notification} Tokens: {Tokens}", JsonSerializer.Serialize(notification), tokens);
            }
        }
    }
}
