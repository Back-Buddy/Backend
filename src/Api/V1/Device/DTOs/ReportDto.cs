namespace BackBuddy.Api.Service.V1.Device.DTOs
{
    public record ReportDto
    {
        public required Guid Id { get; init; }
        public required Guid DeviceId { get; init; }
        public required DateTime StartTime { get; init; }
        public required DateTime EndTime { get; init; }
        public required List<Guid> UsedLogs { get; set; }
        public required ReportMetadataDto Metadata { get; init; }
    }
}
