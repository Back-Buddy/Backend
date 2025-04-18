namespace BackBuddy.Api.Service.V1.Device.DTOs
{
    public record DeviceUpdateRequestDto
    {
        public string? Name { get; init; }
        public TimeSpan? Threshold { get; init; }
    }
}
