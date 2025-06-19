using BackBuddy.Core.Library.Device.Entities;
using BackBuddy.Core.Library.Device.Enums;

namespace BackBuddy.Core.Library.Device.Dtos.Queue.Report
{
    public record ReportAddLikeRequestMessage
    {
        public required string UserId { get; init; }
        public required ReportEntity Report { get; init; }
        public required IEnumerable<ReportVisibilityType> VisibilityTypes { get; init; }
    }

    public record ReportAddLikeResponseMessage
    {
        // Empty response object
    }
}
