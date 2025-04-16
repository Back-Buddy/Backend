namespace BackBuddy.Api.Service.V1.Exceptions.DTOs
{
    public record ErrorDto
    {
        public required string Code { get; init; }
        public required string Message { get; init; }
    }
}
