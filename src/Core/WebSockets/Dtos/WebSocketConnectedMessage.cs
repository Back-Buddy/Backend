namespace BackBuddy.Core.Library.WebSockets.Dtos
{
    public record WebSocketConnectedMessage
    {
        public required Guid DeviceId { get; init; }
    }
}
