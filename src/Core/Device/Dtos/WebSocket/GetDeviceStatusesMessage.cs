namespace BackBuddy.Core.Library.Device.Dtos.WebSocket
{
    public record GetDeviceStatusesRequestMessage
    {
    }

    public record GetDeviceStatusesResponseMessage
    {
        public required IEnumerable<DeviceStatusDto> StatusEntities { get; init; }
    }
}
