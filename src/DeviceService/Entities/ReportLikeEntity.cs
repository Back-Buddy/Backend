namespace BackBuddy.Device.Service.Entities
{
    public record ReportLikeEntity
    {
        public required Guid Id { get; set; }
        public required string UserId { get; set; }
        public required Guid ReportId { get; set; }
    }
}
