namespace BackBuddy.Core.Library.Users.Dtos.Messages
{
    public record UserIsUserIdValidRequestMessage
    {
        public required string UserId { get; init; }
    }

    public record UserIsUserIdValidResponseMessage
    {
        public required bool IsValid { get; init; }
    }
}
