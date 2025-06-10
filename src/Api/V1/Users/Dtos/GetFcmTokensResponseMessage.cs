namespace BackBuddy.Api.Service.V1.Users.Dtos
{
    public record GetFcmTokensResponseMessage
    {
        public required IEnumerable<string> Tokens { get; init; }
    }
}
