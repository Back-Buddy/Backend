using BackBuddy.Core.Library.Device.Enums;

namespace BackBuddy.Device.Service.Entities
{
    public record DeviceLogEntity
    {
        public required Guid Id { get; set; }
        public required Guid DeviceId { get; set; }
        public required DateTime StartTime { get; set; }
        public required DateTime EndTime { get; set; }
        public required DeviceLogType LogType { get; set; }
    }
}
