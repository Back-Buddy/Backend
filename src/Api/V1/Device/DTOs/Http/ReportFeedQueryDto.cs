using BackBuddy.Api.Service.V1.Device.Enums;
using System.ComponentModel;

namespace BackBuddy.Api.Service.V1.Device.DTOs.Http
{
    public record ReportFeedQueryDto
    {
        [DefaultValue(true)]
        public bool Descending { get; init; } = true;
        [DefaultValue(ReportExpandType.None)]
        public ReportExpandType ExpandType { get; init; } = ReportExpandType.None;
    }
}
