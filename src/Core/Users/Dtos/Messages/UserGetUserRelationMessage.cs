namespace BackBuddy.Core.Library.Users.Dtos.Messages
{
    public record UserGetUserRelationRequestMessage
    {
        public required string UserId { get; init; }
        public required string TargetUserId { get; init; }
    }

    public record UserGetUserRelationResponseMessage
    {
        public required UserRelationDto Relation { get; init; }
    }
}
