namespace BackBuddy.Core.Library.Device.Dtos.WebSocket
{
    public record ValidateDeviceStatusRequestMessage
    {
        public required IEnumerable<DeviceStatusDto> StatusEntities { get; init; }
    }

    public record ValidateDeviceStatusResponseMessage
    {
    }
}
