using BackBuddy.Api.Service.V1.Device.Enums;

namespace BackBuddy.Api.Service.V1.Device.DTOs
{
    public record DeviceLogDto
    {
        public required Guid Id { get; set; }
        public required DateTime StartTime { get; set; }
        public required DateTime EndTime { get; set; }
        public required DeviceLogType LogType { get; set; }
    }
}
