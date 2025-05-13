namespace BackBuddy.Api.Service.V1.Device.Entities
{
    public record DeviceEntity
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required string UserId { get; set; }
        public TimeSpan Threshold { get; set; } = TimeSpan.FromMinutes(10);
        public required DateTime SecretGeneratedAt { get; set; }
        public required bool Active { get; set; }
    }
}
