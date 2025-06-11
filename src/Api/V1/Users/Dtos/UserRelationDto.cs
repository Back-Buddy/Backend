namespace BackBuddy.Api.Service.V1.Users.Dtos
{
    public record UserRelationDto
    {
        public required bool IsFollowing { get; init; }
        public required bool IsFollowedBy { get; init; }
    }
}
