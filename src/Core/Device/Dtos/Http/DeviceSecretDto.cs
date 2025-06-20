namespace BackBuddy.Core.Library.Device.Dtos.Http
{
    public record DeviceSecretDto
    {
        public required Guid DeviceId { get; init; }
        public required string Secret { get; init; }
    }
}
