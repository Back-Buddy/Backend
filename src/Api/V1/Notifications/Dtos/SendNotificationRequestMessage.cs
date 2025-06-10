using FirebaseAdmin.Messaging;

namespace BackBuddy.Api.Service.V1.Notifications.Dtos
{
    public record SendNotificationRequestMessage
    {
        public required IEnumerable<string> Tokens { get; init; }
        public required Notification Notification { get; init; }
    }
}
