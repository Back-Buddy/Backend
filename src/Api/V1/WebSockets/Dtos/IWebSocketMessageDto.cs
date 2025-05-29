using BackBuddy.Api.Service.V1.WebSockets.Enums;

namespace BackBuddy.Api.Service.V1.WebSockets.DTOs
{
    public interface IWebSocketMessageDto
    {
        WebSocketMessageType MessageType { get; }

        bool IsToSend { get; }
    }
}
