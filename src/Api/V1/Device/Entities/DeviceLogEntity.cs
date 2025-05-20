using BackBuddy.Api.Service.V1.Device.Enums;

namespace BackBuddy.Api.Service.V1.Device.Entities
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
