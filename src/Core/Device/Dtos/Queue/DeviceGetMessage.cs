namespace BackBuddy.Core.Library.Device.Dtos.Queue
{
    public record DeviceGetRequestMessage
    {
        public required string UserId { get; init; }
        public required Guid DeviceId { get; init; }
    }

    public record DeviceGetResponseMessage
    {
        public required DeviceDto Device { get; init; }
    }
}
