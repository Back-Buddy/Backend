namespace BackBuddy.Api.Service.V1.Notification.Entities
{
    public record NotificationEntity
    {
        public required string UserId { get; set; }
        public required string Token { get; set; }
    }
}