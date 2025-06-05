namespace BackBuddy.Core.Library.Device.Dtos
{
    public record GetDeviceStatusesResponseMessage
    {
        public required IEnumerable<DeviceStatusDto> StatusEntities { get; init; }
    }
}
