namespace BackBuddy.Core.Library.Device.Dtos.Queue.Report
{
    public record ReportUpdateRequestMessage
    {
        public required string UserId { get; init; }
        public required Guid ReportId { get; init; }
        public required ReportUpdateDto Request { get; init; }
    }

    public record ReportUpdateResponseMessage
    {
        // Empty response object
    }
}
