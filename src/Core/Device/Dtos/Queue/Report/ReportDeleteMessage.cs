namespace BackBuddy.Core.Library.Device.Dtos.Queue.Report
{
    public record ReportDeleteRequestMessage
    {
        public required string UserId { get; init; }
        public required Guid ReportId { get; init; }
    }

    public record ReportDeleteResponseMessage
    {
        // Empty response object
    }
}
