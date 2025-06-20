using BackBuddy.Core.Library.Device.Enums;
using System.ComponentModel;

namespace BackBuddy.Core.Library.Device.Dtos.Http
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
