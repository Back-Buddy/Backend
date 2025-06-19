namespace BackBuddy.Core.Library.Users.Dtos.Messages
{
    public record UserDeleteRequestMessage
    {
        public required string UserId { get; init; }
    }

    public record UserDeleteResponseMessage
    {
        // Empty response object
    }
}
