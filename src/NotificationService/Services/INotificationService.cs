namespace BackBuddy.Notification.Service.Services
{
    public interface INotificationService
    {
        Task SendNotification(IEnumerable<string> tokens, FirebaseAdmin.Messaging.Notification notification, CancellationToken cancellationToken = default);
    }
}
