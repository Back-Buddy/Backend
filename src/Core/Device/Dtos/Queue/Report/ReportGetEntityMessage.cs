using BackBuddy.Core.Library.Device.Entities;

namespace BackBuddy.Core.Library.Device.Dtos.Queue.Report
{
    public record ReportGetEntityRequestMessage
    {
        public required Guid ReportId { get; init; }
    }

    public record ReportGetEntityResponseMessage
    {
        public required ReportEntity Report { get; init; }
    }
}
