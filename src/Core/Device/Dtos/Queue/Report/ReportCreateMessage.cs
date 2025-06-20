using BackBuddy.Core.Library.Device.Dtos.Http;

namespace BackBuddy.Core.Library.Device.Dtos.Queue.Report
{
    public record ReportCreateRequestMessage
    {
        public required string UserId { get; init; }
        public required ReportCreateDto Request { get; init; }
    }

    public record ReportCreateResponseMessage
    {
        public required ReportDto Report { get; init; }
    }
}
