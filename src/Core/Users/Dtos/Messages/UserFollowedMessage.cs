namespace BackBuddy.Core.Library.Users.Dtos.Messages
{
    public record UserFollowedMessage
    {
        public required string UserId { get; init; }
        public required string TargetUserId { get; init; }
    }
}
