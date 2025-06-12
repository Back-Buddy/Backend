namespace BackBuddy.Api.Service.V1.Users.Dtos.Messages
{
    public record UserFollowedMessage
    {
        public required string UserId { get; init; }
        public required string TargetUserId { get; init; }
    }
}
