namespace BackBuddy.Api.Service.V1.Users.Dtos
{
    public record GetFcmTokensRequestMessage
    {
        public required string UserId { get; init; }
    }
}
