namespace BackBuddy.Core.Library.Users.Dtos.Messages
{
    public record UserAddRelationRequestMessage
    {
        public required string UserId { get; init; }
        public required string TargetUserId { get; init; }
    }

    public record UserAddRelationResponseMessage
    {
        // Empty response object
    }
}
