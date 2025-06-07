using FirebaseAdmin.Messaging;

namespace BackBuddy.Core.Library.Notifications
{
    public record NotificationDevDebugDto
    {
        public required IEnumerable<string> Tokens { get; init; }
        public required Notification Notification { get; init; }
    }
}
