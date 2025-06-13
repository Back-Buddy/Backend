using System.ComponentModel;

namespace BackBuddy.Api.Service.V1.Device.DTOs.Http
{
    public record ReportQueryDto
    {
        public string? UserId { get; init; } = null;
        public List<Guid> Devices { get; init; } = [];

        [DefaultValue(null)]
        public DateTime? StartTime { get; init; } = null;
        [DefaultValue(null)]
        public DateTime? EndTime { get; init; } = null;

        [DefaultValue(true)]
        public bool Descending { get; init; } = true;
    }
}
