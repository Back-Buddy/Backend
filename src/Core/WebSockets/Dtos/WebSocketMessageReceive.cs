namespace BackBuddy.Core.Library.WebSockets.Dtos
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
