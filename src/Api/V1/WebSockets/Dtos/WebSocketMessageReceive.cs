namespace BackBuddy.Api.Service.V1.WebSockets.DTOs
{
    public record WebSocketMessageReceive<T> where T : IWebSocketMessageDto
    {
        public WebSocketMessageReceive(Guid deviceId, T message)
        {
            DeviceId = deviceId;
            Message = message;
        }

        public Guid DeviceId { get; init; }
        public T Message { get; init; }
    }
}
