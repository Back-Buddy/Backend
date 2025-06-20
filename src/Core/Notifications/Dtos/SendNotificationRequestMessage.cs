using FirebaseAdmin.Messaging;

namespace BackBuddy.Core.Library.Notifications.Dtos
{
    public record SendNotificationRequestMessage
    {
        public required IEnumerable<string> Tokens { get; init; }
        public required Notification Notification { get; init; }
    }
}
