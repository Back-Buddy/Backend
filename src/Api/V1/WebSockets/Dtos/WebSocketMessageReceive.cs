namespace BackBuddy.Api.Service.V1.WebSockets.DTOs
{
    public record WebSocketMessageReceive
    {
        public required Guid DeviceId { get; init; }
        public required IWebSocketMessageDto Message { get; init; }
    }
}
