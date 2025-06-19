namespace BackBuddy.Core.Library.Users.Dtos.Messages
{
    public record UserDeletedMessage
    {
        public required string UserId { get; init; }
    }
}
