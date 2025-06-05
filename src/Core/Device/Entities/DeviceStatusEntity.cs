namespace BackBuddy.Core.Library.Device.Entities
{
    public record DeviceStatusEntity
    {
        public required DateTime StartTime { get; init; }
        public required bool PushSent { get; init; } //TODO: Separate this into a different entity because of parallelism
    }
}
