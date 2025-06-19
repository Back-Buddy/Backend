using BackBuddy.Core.Library.Device.Entities;
using BackBuddy.Core.Library.Device.Enums;

namespace BackBuddy.Core.Library.Device.Dtos.Queue.Report
{
    public record ReportGetVisibilityTypeForUserRequestMessage
    {
        public required string UserId { get; init; }
        public required ReportEntity TargetReport { get; init; }
    }

    public record ReportGetVisibilityTypeForUserResponseMessage
    {
        public required IEnumerable<ReportVisibilityType> VisibilityTypes { get; init; }
    }
}
