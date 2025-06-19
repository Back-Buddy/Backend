namespace BackBuddy.Core.Library.Device.Dtos.Queue
{
    public record DeviceAuthorizeRequestMessage
    {
        public required string Secret { get; init; }
    }
}
