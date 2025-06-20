namespace BackBuddy.Core.Library.Users.Dtos
{
    public record UserRelationDto
    {
        public required bool IsFollowing { get; init; }
        public required bool IsFollowedBy { get; init; }
    }
}
