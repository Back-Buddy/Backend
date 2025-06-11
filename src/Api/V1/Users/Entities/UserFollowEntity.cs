namespace BackBuddy.Api.Service.V1.Users.Entities
{
    public record UserFollowEntity
    {
        public required Guid Id { get; set; }
        public required string UserId { get; set; }
        public required string TargetId { get; set; }
        public required DateTime CreatedAt { get; set; }
    }
}
