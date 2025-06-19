namespace BackBuddy.Core.Library.Users.Dtos.Messages
{
    public record UserRemoveRelationRequestMessage
    {
        public required string UserId { get; init; }
        public required string TargetUserId { get; init; }
    }

    public record UserRemoveRelationResponseMessage
    {
        // Empty response object
    }
}
