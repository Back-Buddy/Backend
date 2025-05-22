namespace BackBuddy.Api.Service.V1.Device.DTOs
{
    public record DeviceDto
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required TimeSpan Threshold { get; set; }
        public required bool Active { get; set; }
        public required bool Online { get; set; }
    }
}
