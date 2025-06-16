using BackBuddy.Api.Service.V1.Device.Enums;

namespace BackBuddy.Api.Service.V1.Device.DTOs
{
    public record ReportDto
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public required ReportVisibilityType? VisibilityType { get; init; } // Only set if the report is retrieved by the user who created it
        public required Guid? DeviceId { get; init; } // Only set if the report is retrieved by the user who created it
        public required DateTime StartTime { get; init; }
        public required DateTime EndTime { get; init; }
        public required List<Guid>? UsedLogsIds { get; set; } // Only set if the report is retrieved by the user who created it
        public required List<DeviceLogDto>? UsedLogs { get; init; }
        public required ReportMetadataDto Metadata { get; init; }
        public required DateTime CreatedAt { get; init; }
        public required long LikeCount { get; init; } = 0;
    }
}
