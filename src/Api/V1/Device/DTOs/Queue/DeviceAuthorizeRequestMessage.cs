namespace BackBuddy.Api.Service.V1.Device.DTOs.Queue
{
    public record DeviceAuthorizeRequestMessage
    {
        public required string Secret { get; init; }
    }
}
