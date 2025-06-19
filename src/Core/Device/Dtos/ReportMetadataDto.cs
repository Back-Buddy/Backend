namespace BackBuddy.Core.Library.Device.Dtos
{
    public record ReportMetadataDto
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
