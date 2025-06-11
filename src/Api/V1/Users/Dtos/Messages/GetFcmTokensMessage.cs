namespace BackBuddy.Api.Service.V1.Users.Dtos.Messages
{
    public record GetFcmTokensMessage
    {
        public required string UserId { get; init; }
    }

    public record GetFcmTokensResponseMessage
    {
        public required IEnumerable<string> Tokens { get; init; }
    }
}
