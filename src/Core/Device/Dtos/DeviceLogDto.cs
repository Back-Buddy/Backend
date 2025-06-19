using BackBuddy.Core.Library.Device.Enums;

namespace BackBuddy.Core.Library.Device.Dtos
{
    public record DeviceLogDto
    {
        public required Guid Id { get; set; }
        public required DateTime StartTime { get; set; }
        public required DateTime EndTime { get; set; }
        public required DeviceLogType LogType { get; set; }
    }
}
