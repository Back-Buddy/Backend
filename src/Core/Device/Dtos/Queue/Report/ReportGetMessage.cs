using BackBuddy.Core.Library.Device.Enums;

namespace BackBuddy.Core.Library.Device.Dtos.Queue.Report
{
    public record ReportGetRequestMessage
    {
        public required string UserId { get; init; }
        public required Guid ReportId { get; init; }
        public required ReportExpandType ExpandType { get; init; }
    }

    public record ReportGetResponseMessage
    {
        public required ReportDto Report { get; init; }
    }
}
