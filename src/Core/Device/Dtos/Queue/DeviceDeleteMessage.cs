namespace BackBuddy.Core.Library.Device.Dtos.Queue
{
    public record DeviceDeleteRequestMessage
    {
        public required string UserId { get; init; }
        public required Guid DeviceId { get; init; }
    }

    public record DeviceDeleteResponseMessage
    {
        // Simple response object with no content
    }
}
