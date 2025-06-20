namespace BackBuddy.Core.Library.WebSockets.Dtos
{
    public record WebSocketDeviceIsOnlineRequest
    {
        public required Guid DeviceId { get; init; }
    }

    public record WebSocketDeviceIsOnlineResponse
    {
        public required bool IsOnline { get; init; }
    }
}