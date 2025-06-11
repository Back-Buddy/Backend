namespace BackBuddy.Api.Service.V1.Users.Dtos
{
    public record UserDto
    {
        public required string UserId { get; init; }
        public required string Username { get; init; }
        public string? Avatar { get; init; } = null;
    }
}
