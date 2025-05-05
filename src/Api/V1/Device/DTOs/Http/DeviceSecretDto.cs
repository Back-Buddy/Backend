namespace BackBuddy.Api.Service.V1.Device.DTOs.Http
{
    public record DeviceSecretDto
    {
        public required Guid DeviceId { get; init; }
        public required string Secret { get; init; }
    }
}
