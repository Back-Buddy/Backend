using BackBuddy.Api.Service.V1.Device.Enums;
using System.ComponentModel;

namespace BackBuddy.Api.Service.V1.Device.DTOs.Http
{
    public record DeviceLogQueryDto
    {
        public DeviceLogType? LogType { get; init; }
        public DateTime? StartTime { get; init; }
        public DateTime? EndTime { get; init; }
        [DefaultValue(true)]
        public bool Descending { get; init; } = true;
    }
}
