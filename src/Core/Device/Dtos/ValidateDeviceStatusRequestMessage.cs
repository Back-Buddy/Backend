namespace BackBuddy.Core.Library.Device.Dtos
{
    public record ValidateDeviceStatusRequestMessage
    {
        public required IEnumerable<DeviceStatusDto> StatusEntities { get; init; }
    }
}
