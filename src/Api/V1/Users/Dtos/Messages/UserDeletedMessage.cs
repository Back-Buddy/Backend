namespace BackBuddy.Api.Service.V1.Users.Dtos.Messages
{
    public record UserDeletedMessage
    {
        public required string UserId { get; init; }
    }
}
