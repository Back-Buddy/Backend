using FirebaseAdmin.Messaging;

namespace BackBuddy.Notification.Service.Services
{
    public partial class NotificationService(FirebaseMessaging messaging) : INotificationService
    {
        private readonly FirebaseMessaging _messaging = messaging;

        public async Task SendNotification(IEnumerable<string> tokens, FirebaseAdmin.Messaging.Notification notification, CancellationToken cancellationToken = default)
        {
            if (!tokens.Any())
                return;

            await _messaging.SendEachAsync(
                tokens.Select(token => new Message
                {
                    Token = token,
                    Notification = notification
                }), cancellationToken: cancellationToken
            );
        }
    }
}