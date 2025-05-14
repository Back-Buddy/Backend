namespace BackBuddy.Api.Service.V1.Device.Entities
{
    public record DeviceStatusEntity
    {
        public required DateTime StartTime { get; init; }
        public required bool PushSent { get; init; } //TODO: Seperate this into a different entity because of parralelism
    }
}
