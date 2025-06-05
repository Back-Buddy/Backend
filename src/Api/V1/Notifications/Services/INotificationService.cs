using FirebaseAdmin.Messaging;

namespace BackBuddy.Api.Service.V1.Notifications.Services
{
    public interface INotificationService
    {
        Task SendNotification(IEnumerable<string> tokens, Notification notification);
    }
}
