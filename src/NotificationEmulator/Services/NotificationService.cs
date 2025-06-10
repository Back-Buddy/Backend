using BackBuddy.Core.Library.Notifications;
using System.Collections.Concurrent;

namespace BackBuddy.Notification.Emulator.Services
{

    public interface INotificationService
    {
        void Clear();
        void AddNotification(NotificationDevDebugDto debugDto);
        IEnumerable<NotificationDevDebugDto> GetNotifications();
    }

    public class NotificationService : INotificationService
    {

        private readonly ConcurrentBag<NotificationDevDebugDto> notificationDevDebugDtos = [];

        public void AddNotification(NotificationDevDebugDto debugDto)
        {
            notificationDevDebugDtos.Add(debugDto);
        }

        public void Clear()
        {
            notificationDevDebugDtos.Clear();
        }

        public IEnumerable<NotificationDevDebugDto> GetNotifications()
        {
            return [.. notificationDevDebugDtos];
        }
    }
}
