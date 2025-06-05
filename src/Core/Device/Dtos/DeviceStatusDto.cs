namespace BackBuddy.Core.Library.Device.Dtos
{
    public record DeviceStatusDto
    {
        public required Guid DeviceId { get; init; }
        public required DateTime StartTime { get; init; }
    }
}
