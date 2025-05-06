namespace BackBuddy.Api.Service.V1.Device.DTOs.Http
{
    public record DeviceUpdateRequestDto
    {
        public string? Name { get; init; }
        public TimeSpan? Threshold { get; init; }
    }
}
