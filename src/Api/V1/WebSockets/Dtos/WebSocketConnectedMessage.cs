using System.Net.WebSockets;

namespace BackBuddy.Api.Service.V1.WebSockets.Dtos
{
    public record WebSocketConnectedMessage
    {
        public required Guid DeviceId { get; init; }
        public required WebSocket WebSocket { get; init; }
    }
}
