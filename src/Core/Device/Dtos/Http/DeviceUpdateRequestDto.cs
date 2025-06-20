namespace BackBuddy.Core.Library.Device.Dtos.Http
{
    public record DeviceUpdateRequestDto
    {
        public string? Name { get; init; }
        public TimeSpan? Threshold { get; init; }
        public bool? Active { get; init; }
    }
}
