namespace BackBuddy.Core.Library.Device.Dtos.Queue
{
    public record DeviceGetDeviceLogRequestMessage
    {
        public required string UserId { get; init; }
        public required Guid DeviceId { get; init; }
        public required Guid LogId { get; init; }
    }

    public record DeviceGetDeviceLogResponseMessage
    {
        public required DeviceLogDto DeviceLog { get; init; }
    }
}
