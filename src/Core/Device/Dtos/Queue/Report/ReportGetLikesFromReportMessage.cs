using BackBuddy.Core.Library.Device.Entities;
using BackBuddy.Core.Library.Device.Enums;
using BackBuddy.Core.Library.Utilities;

namespace BackBuddy.Core.Library.Device.Dtos.Queue.Report
{
    public record ReportGetLikesFromReportRequestMessage
    {
        public required ReportEntity Report { get; init; }
        public required IEnumerable<ReportVisibilityType> VisibilityTypes { get; init; }
        public required PageRequestDto Page { get; init; }
    }

    public record ReportGetLikesFromReportResponseMessage
    {
        public required Page<List<string>> Likes { get; init; }
    }
}
