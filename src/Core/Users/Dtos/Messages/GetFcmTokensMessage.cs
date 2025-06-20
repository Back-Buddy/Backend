namespace BackBuddy.Core.Library.Users.Dtos.Messages
{
    public record GetFcmTokensRequestMessage
    {
        public required string UserId { get; init; }
    }

    public record GetFcmTokensResponseMessage
    {
        public required IEnumerable<string> Tokens { get; init; }
    }
}
