using BackBuddy.Api.Service.V1.Device.Enums;

namespace BackBuddy.Api.Service.V1.Device.DTOs.Http
{
    public record DeviceLogQueryDto
    {
        public DeviceLogType? LogType { get; init; }
        public DateTime? StartTime { get; init; }
        public DateTime? EndTime { get; init; }
    }
}
