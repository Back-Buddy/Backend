namespace BackBuddy.Api.Service.V1.Auth.DTOs
{
    public record AuthConfigDto
    {
        public required string Authority { get; init; }
        public required string ValidIssuer { get; init; }
        public required string ValidAudience { get; init; }
    }
}
