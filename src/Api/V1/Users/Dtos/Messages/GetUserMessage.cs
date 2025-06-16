namespace BackBuddy.Api.Service.V1.Users.Dtos.Messages
{
    public record GetUserRequestMessage
    {
        public required string UserId { get; init; }
    }

    public record GetUserResponseMessage
    {
        public required UserDto User { get; init; }
    }
}
