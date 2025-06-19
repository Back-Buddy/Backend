using BackBuddy.Core.Library.Device.Enums;
using System.ComponentModel;

namespace BackBuddy.Core.Library.Device.Dtos.Http
{
    public record ReportFeedQueryDto
    {
        [DefaultValue(true)]
        public bool Descending { get; init; } = true;
        [DefaultValue(ReportExpandType.None)]
        public ReportExpandType ExpandType { get; init; } = ReportExpandType.None;
    }
}
