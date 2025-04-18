namespace BackBuddy.Api.Service.V1.Device.DTOs
{
    public record DeviceSecretDto
    {
        public required string Secret { get; init; }
    }
}
