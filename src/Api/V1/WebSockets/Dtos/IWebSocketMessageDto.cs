using BackBuddy.Api.Service.V1.WebSockets.Enums;

namespace BackBuddy.Api.Service.V1.WebSockets.Dtos
{
    public interface IWebSocketMessageDto
    {
        WebSocketMessageType MessageType { get; }

        bool IsToSend();
    }
}
