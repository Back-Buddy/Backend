using BackBuddy.Api.Service.V1.Device.Enums;

namespace BackBuddy.Api.Service.V1.Device.Entities
{
    public record ReportEntity
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required ReportVisibilityType VisibilityType { get; set; }
        public required string UserId { get; set; }
        public required Guid DeviceId { get; set; }
        public required DateTime StartTime { get; set; }
        public required DateTime EndTime { get; set; }
        public required ReportMetadataEntity Metadata { get; set; }
        public required List<Guid> UsedLogs { get; set; }
        public required DateTime CreatedAt { get; set; }
    }

    public record ReportMetadataEntity
    {
        public required TimeSpan TotalTime { get; set; }
        public required TimeSpan SitTime { get; set; }
        public required TimeSpan StandTime { get; set; }
        public required double SitPercentage { get; set; }
        public required double StandPercentage { get; set; }
        public required int PostureChanges { get; set; }
        public required TimeSpan? AverageSitPeriod { get; set; }
        public required TimeSpan? ShortestSitPeriod { get; set; }
        public required TimeSpan? LongestSitPeriod { get; set; }
    }
}
