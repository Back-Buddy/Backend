namespace BackBuddy.Api.Service.V1.WebSockets.Dtos
{
    public record WebSocketMessageReceive
    {
        public required Guid DeviceId { get; init; }
        public required IWebSocketMessageDto Message { get; init; }
    }
}
